using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;
using Frends.AzureEventHub.UpdateCheckpoint.Helpers;

namespace Frends.AzureEventHub.UpdateCheckpoint;

/// <summary>
/// Task class.
/// </summary>
public static class AzureEventHub
{
    /// <summary>
    /// Task to rewind checkpoints in an Azure Storage container for a specified Event Hub consumer group.
    /// Supports relative rollback by a number of events and absolute per-partition targeting by
    /// sequence number or enqueued time. Checkpoints are written via the supported
    /// BlobCheckpointStore API and the partition's current sequence range is resolved by the Task.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.AzureEventHub.UpdateCheckpoint)
    /// </summary>
    /// <param name="input">Event Hub name, consumer group, partition, and rewind targets.</param>
    /// <param name="connection">Storage and authentication configuration.</param>
    /// <param name="options">Optional behavior configurations.</param>
    /// <param name="cancellationToken">A cancellation token provided by the Frends platform.</param>
    /// <returns>
    /// Result { bool Success, string[] UpdatedPartitions, string[] SkippedPartitions, bool RollbackApplied, Error[] Errors, Error Error, AppliedTarget[] AppliedTargets }
    /// </returns>
    public static async Task<Result> UpdateCheckpoint(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        var updatedPartitions = new List<string>();
        var skippedPartitions = new List<string>();
        var errorDetails = new List<Error>();
        var appliedTargets = new List<AppliedTarget>();
        var rollbackApplied = false;

        try
        {
            ValidationHandler.Run(input, connection);
            ValidationHandler.Run(input.Targets.Cast<object>().ToArray());

            if (connection.AuthMethod == AuthMethod.OAuth && connection.OAuth == null)
                throw new ArgumentException("OAuth configuration must be provided when using OAuth auth method.");

            if (connection.EventHubAuthMethod == AuthMethod.OAuth && connection.OAuth == null)
                throw new ArgumentException("OAuth configuration must be provided when using OAuth Event Hub auth method.");

            var containerClient = CreateContainerClient(connection);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var checkpointStore = new BlobCheckpointStore(containerClient);
            await using var consumer = CreateConsumerClient(connection, input.EventHubName);

            foreach (var target in input.Targets)
            {
                var partitionId = target.PartitionId;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    PartitionProperties partitionProperties;
                    try
                    {
                        partitionProperties = await consumer.GetPartitionPropertiesAsync(partitionId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) throw;

                        throw new InvalidOperationException(
                            $"Partition '{partitionId}' could not be resolved on Event Hub '{input.EventHubName}'. Verify the partition ID exists.", ex);
                    }

                    var previousSequence = await TryGetExistingCheckpointSequenceAsync(
                        containerClient, connection.EventHubNamespace, input.EventHubName, input.ConsumerGroup, partitionId, cancellationToken);

                    if (options.FailIfPartitionOwned &&
                        await IsPartitionOwnedAsync(containerClient, connection.EventHubNamespace, input.EventHubName, input.ConsumerGroup, partitionId, cancellationToken))
                    {
                        throw new InvalidOperationException(
                            $"Partition '{partitionId}' is currently owned by a running consumer. Stop the consuming Process before rewinding, or set Options.FailIfPartitionOwned to false to override.");
                    }

                    long targetSequence;
                    string targetOffset;

                    switch (target.Mode)
                    {
                        case TargetMode.AbsoluteEnqueuedTime:
                            (targetSequence, targetOffset) = await ResolvePositionAsync(
                                connection, input, partitionId, EventPosition.FromEnqueuedTime(target.TargetEnqueuedTime.Value), cancellationToken);
                            break;

                        case TargetMode.AbsoluteSequenceNumber:
                            targetSequence = target.TargetSequenceNumber.Value;
                            ValidateInRange(partitionId, targetSequence, partitionProperties);
                            targetOffset = await ResolveOffsetForSequenceAsync(connection, input, partitionId, targetSequence, cancellationToken);
                            break;

                        case TargetMode.RelativeRollback:
                            if (!previousSequence.HasValue)
                            {
                                throw new PartitionMissingException(
                                    $"Checkpoint for partition '{partitionId}' has no current sequence number to roll back from.");
                            }

                            targetSequence = Math.Max(partitionProperties.BeginningSequenceNumber, previousSequence.Value - target.RollbackEvents);

                            ValidateInRange(partitionId, targetSequence, partitionProperties);
                            targetOffset = await ResolveOffsetForSequenceAsync(connection, input, partitionId, targetSequence, cancellationToken);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(target.Mode), "Invalid target mode.");
                    }

                    ValidateInRange(partitionId, targetSequence, partitionProperties);

                    await checkpointStore.UpdateCheckpointAsync(
                        connection.EventHubNamespace,
                        input.EventHubName,
                        input.ConsumerGroup,
                        partitionId,
                        string.Empty,
                        new CheckpointPosition(targetOffset, targetSequence),
                        cancellationToken);

                    if (previousSequence.HasValue && targetSequence < previousSequence.Value)
                        rollbackApplied = true;

                    updatedPartitions.Add(partitionId);
                    appliedTargets.Add(new AppliedTarget
                    {
                        PartitionId = partitionId,
                        PreviousSequenceNumber = previousSequence,
                        NewSequenceNumber = targetSequence,
                    });
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) throw;

                    skippedPartitions.Add(partitionId);
                    errorDetails.Add(new Error
                    {
                        Message = $"Failed to update checkpoint for partition {partitionId}: {ex.Message}",
                        AdditionalInfo = ex,
                    });

                    if (ex is PartitionMissingException && options.FailIfPartitionMissing)
                    {
                        break;
                    }
                }
            }

            if (errorDetails.Count > 0)
            {
                var combinedMessage = string.Join("\n", errorDetails.Select(e => e.Message));
                throw new Exception(
                    $"Failed to update one or more checkpoints.\n{combinedMessage}",
                    new AggregateException(errorDetails.Select(e => e.AdditionalInfo as Exception)));
            }

            return new Result
            {
                Success = true,
                UpdatedPartitions = updatedPartitions.ToArray(),
                SkippedPartitions = skippedPartitions.ToArray(),
                RollbackApplied = rollbackApplied,
                Errors = Array.Empty<Error>(),
                AppliedTargets = appliedTargets.ToArray(),
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(
                options,
                updatedPartitions.ToArray(),
                skippedPartitions.ToArray(),
                rollbackApplied,
                appliedTargets.ToArray());
        }
    }

    private static BlobContainerClient CreateContainerClient(Connection connection) => connection.AuthMethod switch
    {
        AuthMethod.ConnectionString => new BlobContainerClient(connection.ConnectionString, connection.ContainerName),
        AuthMethod.SasToken => new BlobContainerClient(
            new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{connection.ContainerName}?{connection.SasToken}")),
        AuthMethod.OAuth => new BlobContainerClient(
            new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{connection.ContainerName}"),
            new ClientSecretCredential(connection.OAuth.TenantId, connection.OAuth.ClientId, connection.OAuth.ClientSecret)),
        _ => throw new ArgumentOutOfRangeException(nameof(connection.AuthMethod), "Invalid auth method"),
    };

    private static EventHubConsumerClient CreateConsumerClient(Connection connection, string eventHubName) => connection.EventHubAuthMethod switch
    {
        AuthMethod.ConnectionString => new EventHubConsumerClient(
            EventHubConsumerClient.DefaultConsumerGroupName, connection.EventHubConnectionString, eventHubName),
        AuthMethod.SasToken => new EventHubConsumerClient(
            EventHubConsumerClient.DefaultConsumerGroupName, connection.EventHubNamespace, eventHubName, new AzureSasCredential(connection.EventHubSasToken)),
        AuthMethod.OAuth => new EventHubConsumerClient(
            EventHubConsumerClient.DefaultConsumerGroupName,
            connection.EventHubNamespace,
            eventHubName,
            new ClientSecretCredential(connection.OAuth.TenantId, connection.OAuth.ClientId, connection.OAuth.ClientSecret)),
        _ => throw new ArgumentOutOfRangeException(nameof(connection.EventHubAuthMethod), "Invalid Event Hub auth method"),
    };

    private static PartitionReceiver CreatePartitionReceiver(Connection connection, string eventHubName, string consumerGroup, string partitionId, EventPosition position)
    {
        // Setting an OwnerLevel makes this an epoch receiver. Without it, the broker rejects the
        // connection outright if any epoch receiver (e.g. an EventProcessorClient) is, or was very
        // recently, attached to this partition/consumer group - even transiently during teardown.
        // Using epoch 0 lets us connect and preempt a stale link instead of failing.
        var options = new PartitionReceiverOptions { OwnerLevel = 0 };

        return connection.EventHubAuthMethod switch
        {
            AuthMethod.ConnectionString => new PartitionReceiver(
                consumerGroup, partitionId, position, connection.EventHubConnectionString, eventHubName, options),
            AuthMethod.SasToken => new PartitionReceiver(
                consumerGroup, partitionId, position, connection.EventHubNamespace, eventHubName, new AzureSasCredential(connection.EventHubSasToken), options),
            AuthMethod.OAuth => new PartitionReceiver(
                consumerGroup,
                partitionId,
                position,
                connection.EventHubNamespace,
                eventHubName,
                new ClientSecretCredential(connection.OAuth.TenantId, connection.OAuth.ClientId, connection.OAuth.ClientSecret),
                options),
            _ => throw new ArgumentOutOfRangeException(nameof(connection.EventHubAuthMethod), "Invalid Event Hub auth method"),
        };
    }

    private static async Task<(long Sequence, string Offset)> ResolvePositionAsync(
        Connection connection, Input input, string partitionId, EventPosition position, CancellationToken cancellationToken)
    {
        await using var receiver = CreatePartitionReceiver(connection, input.EventHubName, input.ConsumerGroup, partitionId, position);
        var events = await receiver.ReceiveBatchAsync(1, TimeSpan.FromSeconds(30), cancellationToken);
        var evt = events.FirstOrDefault();
        if (evt == null)
        {
            throw new InvalidOperationException(
                $"No event found at the requested position for partition '{partitionId}'. The target may be beyond the last enqueued event.");
        }

        return (evt.SequenceNumber, evt.OffsetString);
    }

    private static async Task<string> ResolveOffsetForSequenceAsync(
        Connection connection, Input input, string partitionId, long sequenceNumber, CancellationToken cancellationToken)
    {
        // Read the event at the target sequence (exclusive start at seq-1) to obtain a consistent offset,
        // so the written CheckpointPosition resumes deterministically regardless of offset/sequence precedence.
        var start = EventPosition.FromSequenceNumber(sequenceNumber - 1, isInclusive: false);
        var (resolvedSequence, offset) = await ResolvePositionAsync(connection, input, partitionId, start, cancellationToken);

        if (resolvedSequence != sequenceNumber)
        {
            throw new InvalidOperationException(
                $"Could not resolve offset for sequence number {sequenceNumber} on partition '{partitionId}'.");
        }

        return offset;
    }

    private static void ValidateInRange(string partitionId, long sequence, PartitionProperties properties)
    {
        if (sequence < properties.BeginningSequenceNumber || sequence > properties.LastEnqueuedSequenceNumber)
        {
            var message = $"Target sequence number {sequence} for partition '{partitionId}' is outside the valid range " +
                $"[{properties.BeginningSequenceNumber}, {properties.LastEnqueuedSequenceNumber}].";
            throw new ArgumentOutOfRangeException(nameof(sequence), message);
        }
    }

    private static async Task<long?> TryGetExistingCheckpointSequenceAsync(
        BlobContainerClient containerClient, string eventHubNamespace, string eventHubName, string consumerGroup, string partitionId, CancellationToken cancellationToken)
    {
        var blobName = $"{eventHubNamespace}/{eventHubName}/{consumerGroup}/checkpoint/{partitionId}".ToLowerInvariant();
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        if (properties.Value.Metadata.TryGetValue("sequencenumber", out var seqStr) && long.TryParse(seqStr, out var seq))
        {
            return seq;
        }

        return null;
    }

    private static async Task<bool> IsPartitionOwnedAsync(
        BlobContainerClient containerClient, string eventHubNamespace, string eventHubName, string consumerGroup, string partitionId, CancellationToken cancellationToken)
    {
        var blobName = $"{eventHubNamespace}/{eventHubName}/{consumerGroup}/ownership/{partitionId}".ToLowerInvariant();
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return false;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        // Ownership is renewed roughly every 10 seconds by an active processor. Treat a recently
        // modified ownership record as an actively owned partition.
        return DateTimeOffset.UtcNow - properties.Value.LastModified < TimeSpan.FromSeconds(60);
    }
}
