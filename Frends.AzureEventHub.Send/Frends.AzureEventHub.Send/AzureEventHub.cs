using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Frends.AzureEventHub.Send.Definitions;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.AzureEventHub.Send;

/// <summary>
/// Azure EventHub Send task.
/// </summary>
public static class AzureEventHub
{
    /// <summary>
    /// Reads text and replaces substring(s) matching with specified regular expression.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.AzureEventHub.Send)
    /// </summary>
    /// <returns>{ bool Success, string Message }</returns>
    public static async Task<Result> Send(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var eventHubProducerClientOptions = CreateOptions(options);
        var producerClient = new EventHubProducerClient(
            input.ConnectionString,
            eventHubProducerClientOptions);

        // Create a batch of events
        using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

        for (int i = 0; i < input.Messages.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new Result(false, "Task was cancelled.");
            }

            var messageBytes = Encoding.UTF8.GetBytes(input.Messages[i].Message);
            var eventData = new EventData(messageBytes);
            if (!eventBatch.TryAdd(eventData))
            {
                // if it is too large for the batch
                throw new InvalidOperationException(
                    $"Event {i} is too large for the batch; " +
                    $"maximum batch size is {eventBatch.MaximumSizeInBytes} bytes, current batch size is {eventBatch.SizeInBytes} bytes and message size is {messageBytes.Length} bytes.");
            }
        }

        try
        {
            // Use the producer client to send the batch of events to the event hub
            await producerClient.SendAsync(eventBatch, cancellationToken);
            return new Result(
                true,
                $"A batch of {input.Messages.Length} events has been published.");
        }
        catch (Exception ex)
        {
            return new Result(
                false,
                $"An error occurred while publishing events: {ex.Message}");
        }
        finally
        {
            await producerClient.DisposeAsync();
        }
    }

    internal static EventHubProducerClientOptions CreateOptions(Options options)
    {
        return new EventHubProducerClientOptions
        {
            ConnectionOptions = new EventHubConnectionOptions
            {
                TransportType = options.GetNativeClientTransportType()
            },
            RetryOptions = new EventHubsRetryOptions
            {
                MaximumRetries = options.MaximumRetries,
                Delay = TimeSpan.FromMilliseconds(options.DelayInMilliseconds),
                MaximumDelay = TimeSpan.FromMilliseconds(options.MaximumDelayInMilliseconds),
                TryTimeout = TimeSpan.FromMilliseconds(options.TryTimeoutInMilliseconds),
                Mode = options.GetNativeClientRetryMode()
            }
        };
    }
}