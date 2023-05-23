using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Frends.AzureEventHub.Receive.Definitions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    /// <returns>Object { bool Success, List&lt;dynamic&gt; Data }</returns>
    public static async Task<Result> Receive([PropertyTab] Consumer consumer, [PropertyTab] Checkpoint checkpoint, [PropertyTab] Options options, CancellationToken cancellationToken)
    {
        if (options.MaxEvents.Equals(0) && options.MaximumWaitTimeInMinutes.Equals(0))
            throw new Exception("Both Options.MaxEvents and Options.MaximumWaitTimeInMinutes cannot be unlimited.");

        var results = new List<dynamic>();
        var stopProcessing = false;
        EventProcessorClient processorClient = null;
        var timeOut = options.MaximumWaitTimeInMinutes > 0 ? DateTime.UtcNow.AddMinutes(options.MaximumWaitTimeInMinutes) : DateTime.UtcNow;
        var cancellationTokenSource = new CancellationTokenSource();


        try
        {
            var checkpointStorageClient = CreateBlobContainerClient(checkpoint);

            if (checkpoint.CreateContainer && checkpoint.AuthenticationMethod != AuthenticationMethod.SASToken)
                await checkpointStorageClient.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellationToken);

            processorClient = CreateEventProcessorClient(consumer, checkpointStorageClient);

            processorClient.ProcessEventAsync += async (args) =>
            {
                if (options.MaximumWaitTimeInMinutes > 0 && timeOut <= DateTime.UtcNow)
                    stopProcessing = true;
                else if (options.MaxEvents > 0 && results.Count >= options.MaxEvents)
                    stopProcessing = true;
                else if (args.Data != null && !stopProcessing)
                {
                    var result = Encoding.UTF8.GetString(args.Data.Body.ToArray());
                    results.Add(result);
                    await args.UpdateCheckpointAsync(cancellationToken);
                }
            };

            processorClient.ProcessErrorAsync += async (args) =>
            {
                var ex = @$"Partition {args.PartitionId}, Exception: {args.Exception}";

                if (options.ExceptionHandler is ExceptionHandlers.Info)
                {
                    results.Add(ex);
                    await Task.CompletedTask;
                }
                else
                    throw new Exception(ex);
            };

            await processorClient.StartProcessingAsync(cancellationToken);

            while (!stopProcessing)
                await Task.Delay(TimeSpan.FromSeconds(options.Delay), cancellationToken);

            //if (processorClient.IsRunning)
            //    await processorClient.StopProcessingAsync(cancellationToken);

        }
        catch (Exception ex)
        {
            if (options.ExceptionHandler is ExceptionHandlers.Throw)
                throw new Exception($@"{ex}");
            else
            {
                results.Add(@$"An exception occured: {ex}");
                return new Result(false, results);
            }
        }
        finally
        {
            if (processorClient.IsRunning)
                await processorClient.StopProcessingAsync(cancellationToken);

        }

        return new Result(true, results);
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
            MaximumWaitTime = TimeSpan.FromSeconds(consumer.MaximumWaitTime)
        };

        switch (consumer.AuthenticationMethod)
        {
            case AuthenticationMethod.ConnectionString:
                if (!string.IsNullOrWhiteSpace(consumer.EventHubName))
                    return new(checkpointStorageClient, consumerGroup, consumer.ConnectionString, consumer.EventHubName, eventProcessorClientOptions);
                else
                    return new(checkpointStorageClient, consumerGroup, consumer.ConnectionString, eventProcessorClientOptions);
            case AuthenticationMethod.SASToken:
                return new(checkpointStorageClient, consumerGroup, consumer.FullyQualifiedNamespace, consumer.EventHubName, new AzureSasCredential(consumer.SASToken), eventProcessorClientOptions);
            case AuthenticationMethod.OAuth2:
                return new(checkpointStorageClient, consumerGroup, consumer.FullyQualifiedNamespace, consumer.EventHubName, new ClientSecretCredential(consumer.TenantId, consumer.ClientId, consumer.ClientSecret), eventProcessorClientOptions);
            default:
                throw new Exception("AuthenticationMethod not supported.");
        }
    }
}