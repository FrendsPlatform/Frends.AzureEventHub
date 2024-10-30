using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Frends.AzureEventHub.Receive.Definitions;
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
    /// <param name="consumer">Consumer parameters.</param>
    /// <param name="checkpoint">Checkpoint parameters.</param>
    /// <param name="options">Optional parameters.</param>
    /// <param name="cancellationToken">Token received from Frends to cancel this Task.</param>
    /// <returns>Object { bool Success, List&lt;dynamic&gt; Data, List&lt;dynamic&gt; Errors }</returns>
    public static async Task<Result> Receive([PropertyTab] Consumer consumer, [PropertyTab] Checkpoint checkpoint, [PropertyTab] Options options, CancellationToken cancellationToken)
    {
        if (options.MaxEvents.Equals(0) && options.MaxRunTime.Equals(0))
            throw new Exception("Both Options.MaxEvents and Options.MaxRunTime cannot be unlimited.");
        if (options.MaxRunTime > 0 && consumer.MaximumWaitTime > options.MaxRunTime)
            throw new Exception("Consumer.MaximumWaitTime cannot exceed Options.MaxRunTime when Options.MaxRunTime is greater than 0.");

        var results = new ConcurrentBag<dynamic>();
        var errors = new ConcurrentBag<dynamic>();
        var stopProcessing = false;
        EventProcessorClient processorClient = null;
        var timeOut = options.MaxRunTime > 0 ? DateTime.UtcNow.AddSeconds(options.MaxRunTime) : DateTime.UtcNow;
        var maximumWaitTime = consumer.MaximumWaitTime > 0 ? TimeSpan.FromSeconds(consumer.MaximumWaitTime) : (TimeSpan?)null;
        var lastEventTime = DateTime.UtcNow;

        async Task ProcessEventHandler(ProcessEventArgs args)
        {
            if (args.Data is not null)
            {
                results.Add(Encoding.UTF8.GetString(args.Data.Body.ToArray()));
                await args.UpdateCheckpointAsync(cancellationToken);
                lastEventTime = DateTime.UtcNow;

                if (options.MaxRunTime > 0 && timeOut <= DateTime.UtcNow || options.MaxEvents > 0 && results.Count >= options.MaxEvents)
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
            var checkpointStorageClient = CreateBlobContainerClient(checkpoint);

            if (checkpoint.CreateContainer && checkpoint.AuthenticationMethod is not AuthenticationMethod.SASToken)
                await checkpointStorageClient.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellationToken);

            processorClient = CreateEventProcessorClient(consumer, checkpointStorageClient);

            processorClient.ProcessEventAsync += ProcessEventHandler;
            processorClient.ProcessErrorAsync += ProcessErrorHandler;

            await processorClient.StartProcessingAsync(cancellationToken);

            while (!stopProcessing)
            {
                if (maximumWaitTime.HasValue && DateTime.UtcNow - lastEventTime >= maximumWaitTime.Value)
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
                throw;

            errors.Add($"An exception occurred: {ex}");
            return new Result(false, results, errors);
        }
        finally
        {
            if (processorClient != null)
            {
                processorClient.ProcessEventAsync -= ProcessEventHandler;
                processorClient.ProcessErrorAsync -= ProcessErrorHandler;
                await processorClient.StopProcessingAsync(cancellationToken);
            }
        }

        return new Result(true, results, errors);
    }

    private static BlobContainerClient CreateBlobContainerClient(Checkpoint checkpoint)
    {
        return checkpoint.AuthenticationMethod switch
        {
            AuthenticationMethod.ConnectionString => new(checkpoint.ConnectionString, checkpoint.ContainerName),
            AuthenticationMethod.SASToken => new(new Uri(checkpoint.BlobContainerUri), new AzureSasCredential(checkpoint.SASToken)),
            AuthenticationMethod.OAuth2 => new(new Uri(checkpoint.BlobContainerUri), new ClientSecretCredential(checkpoint.TenantId, checkpoint.ClientId, checkpoint.ClientSecret)),
            _ => throw new Exception("Authentication method not supported."),
        };
    }

    private static EventProcessorClient CreateEventProcessorClient(Consumer consumer, BlobContainerClient checkpointStorageClient)
    {
        var consumerGroup = !string.IsNullOrWhiteSpace(consumer.ConsumerGroup) ? consumer.ConsumerGroup : EventHubConsumerClient.DefaultConsumerGroupName;

        EventProcessorClientOptions eventProcessorClientOptions = new()
        {
            MaximumWaitTime = consumer.MaximumWaitTime > 0 ? TimeSpan.FromSeconds(consumer.MaximumWaitTime) : null
        };

        switch (consumer.AuthenticationMethod)
        {
            case AuthenticationMethod.ConnectionString:
                if (!string.IsNullOrWhiteSpace(consumer.EventHubName))
                    return new(checkpointStorageClient, consumerGroup, consumer.ConnectionString, consumer.EventHubName, eventProcessorClientOptions);
                else
                    return new(checkpointStorageClient, consumerGroup, consumer.ConnectionString, eventProcessorClientOptions);
            case AuthenticationMethod.SASToken:
                return new(checkpointStorageClient, consumerGroup, consumer.Namespace, consumer.EventHubName, new AzureSasCredential(consumer.SASToken), eventProcessorClientOptions);
            case AuthenticationMethod.OAuth2:
                return new(checkpointStorageClient, consumerGroup, consumer.Namespace, consumer.EventHubName, new ClientSecretCredential(consumer.TenantId, consumer.ClientId, consumer.ClientSecret), eventProcessorClientOptions);
            default:
                throw new Exception("AuthenticationMethod not supported.");
        }
    }
}