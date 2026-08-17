using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Frends.AzureEventHub.Receive.Definitions;
using Frends.AzureEventHub.Receive.Helpers;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace Frends.AzureEventHub.Receive;

/// <summary>
/// Azure EventHub Receive Task.
/// </summary>
public static class AzureEventHub
{
    /// <summary>
    /// Receive events from Azure Event Hub.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.AzureEventHub.Receive)
    /// </summary>
    /// <param name="input">Input parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Optional parameters.</param>
    /// <param name="cancellationToken">Token received from Frends to cancel this Task.</param>
    /// <returns>Object { bool Success, List&lt;dynamic&gt; Data, List&lt;dynamic&gt; Errors, Error Error }</returns>
    public static async Task<Result> Receive([PropertyTab] Input input, [PropertyTab] Connection connection, [PropertyTab] Options options, CancellationToken cancellationToken)
    {
        if (options.MaxEvents.Equals(0) && options.MaxRunTime.Equals(0))
            throw new Exception("Both Options.MaxEvents and Options.MaxRunTime cannot be unlimited.");
        if (options.MaxRunTime > 0 && input.MaximumWaitTime > options.MaxRunTime)
            throw new Exception(
                "Input.MaximumWaitTime cannot exceed Options.MaxRunTime when Options.MaxRunTime is greater than 0.");
        if (options.ConsumeAttemptDelay < 0.1)
            throw new Exception("Options.ConsumeAttemptDelay must be at least 0.1 seconds.");

        var results = new ConcurrentBag<dynamic>();
        var errors = new ConcurrentBag<dynamic>();
        var stopProcessing = false;
        EventProcessorClient processorClient = null;
        var timeOut = options.MaxRunTime > 0 ? DateTime.UtcNow.AddSeconds(options.MaxRunTime) : DateTime.UtcNow;
        var maximumWaitTime = input.MaximumWaitTime > 0 ? TimeSpan.FromSeconds(input.MaximumWaitTime) : (TimeSpan?)null;
        var lastEventTime = DateTime.UtcNow;

        async Task ProcessEventHandler(ProcessEventArgs args)
        {
            if (args.Data is not null)
            {
                results.Add(Encoding.UTF8.GetString(args.Data.Body.ToArray()));
                await args.UpdateCheckpointAsync(cancellationToken);
                lastEventTime = DateTime.UtcNow;

                if (options.MaxEvents > 0 && results.Count >= options.MaxEvents)
                    stopProcessing = true;
            }
        }

        async Task ProcessErrorHandler(ProcessErrorEventArgs args)
        {
            if (options.ExceptionHandler is ExceptionHandlers.Throw)
                throw new Exception($"Error occurred in partition {args.PartitionId}: ", args.Exception);

            errors.Add($"Partition {args.PartitionId}, Exception: {args.Exception}");
            stopProcessing = true;
        }

        try
        {
            var checkpointStorageClient = CreateBlobContainerClient(input, connection);

            if (input.CreateContainer && connection.StorageAuthenticationMethod is not AuthenticationMethod.SASToken)
                await checkpointStorageClient.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellationToken);

            processorClient = CreateEventProcessorClient(input, connection, checkpointStorageClient);

            processorClient.ProcessEventAsync += ProcessEventHandler;
            processorClient.ProcessErrorAsync += ProcessErrorHandler;

            await processorClient.StartProcessingAsync(cancellationToken);

            while (!stopProcessing)
            {
                if ((options.MaxRunTime > 0 && timeOut <= DateTime.UtcNow) ||
                    (maximumWaitTime.HasValue &&
                     DateTime.UtcNow - lastEventTime >=
                     maximumWaitTime.Value))
                {
                    stopProcessing = true;
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(options.ConsumeAttemptDelay), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (options.ExceptionHandler == ExceptionHandlers.Throw)
                return ex.Handle(options);

            errors.Add($"An exception occurred: {ex}");
            return new Result(false, results, errors);
        }
        finally
        {
            if (processorClient != null)
            {
                await processorClient.StopProcessingAsync(cancellationToken);
                processorClient.ProcessEventAsync -= ProcessEventHandler;
                processorClient.ProcessErrorAsync -= ProcessErrorHandler;
            }
        }

        return new Result(true, results, errors);
    }

    private static BlobContainerClient CreateBlobContainerClient(Input input, Connection connection)
    {
        return connection.StorageAuthenticationMethod switch
        {
            AuthenticationMethod.ConnectionString => new(connection.StorageConnectionString, input.ContainerName),
            AuthenticationMethod.SASToken => new(new Uri(connection.BlobContainerUri), new AzureSasCredential(connection.StorageSASToken)),
            AuthenticationMethod.OAuth2 => new(new Uri(connection.BlobContainerUri), new ClientSecretCredential(connection.StorageTenantId, connection.StorageClientId, connection.StorageClientSecret)),
            _ => throw new Exception("Authentication method not supported."),
        };
    }

    private static EventProcessorClient CreateEventProcessorClient(Input input, Connection connection, BlobContainerClient checkpointStorageClient)
    {
        var consumerGroup = !string.IsNullOrWhiteSpace(input.ConsumerGroup) ? input.ConsumerGroup : EventHubConsumerClient.DefaultConsumerGroupName;

        EventProcessorClientOptions eventProcessorClientOptions = new()
        {
            MaximumWaitTime = input.MaximumWaitTime > 0 ? TimeSpan.FromSeconds(input.MaximumWaitTime) : null
        };

        switch (connection.EventHubAuthenticationMethod)
        {
            case AuthenticationMethod.ConnectionString:
                if (!string.IsNullOrWhiteSpace(input.EventHubName))
                    return new(checkpointStorageClient, consumerGroup, connection.EventHubConnectionString, input.EventHubName, eventProcessorClientOptions);
                else
                    return new(checkpointStorageClient, consumerGroup, connection.EventHubConnectionString, eventProcessorClientOptions);
            case AuthenticationMethod.SASToken:
                return new(checkpointStorageClient, consumerGroup, connection.EventHubNamespace, input.EventHubName, new AzureSasCredential(connection.EventHubSASToken), eventProcessorClientOptions);
            case AuthenticationMethod.OAuth2:
                return new(checkpointStorageClient, consumerGroup, connection.EventHubNamespace, input.EventHubName, new ClientSecretCredential(connection.EventHubTenantId, connection.EventHubClientId, connection.EventHubClientSecret), eventProcessorClientOptions);
            default:
                throw new Exception("AuthenticationMethod not supported.");
        }
    }
}

