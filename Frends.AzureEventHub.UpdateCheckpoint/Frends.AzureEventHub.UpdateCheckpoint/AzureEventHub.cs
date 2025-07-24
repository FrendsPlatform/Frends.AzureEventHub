using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;
using Frends.AzureEventHub.UpdateCheckpoint.Helpers;
using static Frends.AzureEventHub.UpdateCheckpoint.Definitions.Enums;

namespace Frends.AzureEventHub.UpdateCheckpoint;

/// <summary>
/// Task class.
/// </summary>
public static class AzureEventHub
{
    /// <summary>
    /// Task to update checkpoints in an Azure Storage container for a specified Event Hub consumer group.
    /// Supports selective partition checkpointing and rollback by a specified number of events.
    /// Compatible with connection string, SAS token, and OAuth authentication methods.
    /// </summary>
    /// <param name="input">Event Hub name, consumer group, partition, and rollback details.</param>
    /// <param name="connection">Storage and authentication configuration.</param>
    /// <param name="options">Optional behavior configurations.</param>
    /// <param name="cancellationToken">A cancellation token provided by the Frends platform.</param>
    /// <returns>
    /// Result { string[] UpdatedPartitions, string[] SkippedPartitions, bool RollbackApplied, ErrorDetail[] Errors }
    /// </returns>
    public static async Task<Result> UpdateCheckpoint(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.EventHubName))
                throw new ArgumentException("EventHubName is required", nameof(input.EventHubName));

            if (string.IsNullOrWhiteSpace(input.ConsumerGroup))
                throw new ArgumentException("ConsumerGroup is required", nameof(input.ConsumerGroup));

            if (input.PartitionIds == null || input.PartitionIds.Length == 0)
                throw new ArgumentException("At least one PartitionId is required", nameof(input.PartitionIds));

            switch (connection.AuthMethod)
            {
                case AuthMethod.ConnectionString:
                    if (string.IsNullOrWhiteSpace(connection.ConnectionString))
                        throw new ArgumentException("ConnectionString must be provided when using ConnectionString auth method.");
                    break;

                case AuthMethod.SasToken:
                    if (string.IsNullOrWhiteSpace(connection.SasToken))
                        throw new ArgumentException("SasToken must be provided when using SasToken auth method.");
                    break;

                case AuthMethod.OAuth:
                    if (connection.OAuth == null)
                        throw new ArgumentException("OAuth configuration must be provided when using OAuth auth method.");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(connection.AuthMethod), "Invalid authentication method.");
            }

            var updatedPartitions = new List<string>();
            var skippedPartitions = new List<string>();
            var errorDetails = new List<Error>();
            bool rollbackApplied = input.RollbackEvents > 0;

            BlobContainerClient containerClient = connection.AuthMethod switch
            {
                AuthMethod.ConnectionString => new BlobContainerClient(connection.ConnectionString, connection.ContainerName),
                AuthMethod.SasToken => new BlobContainerClient(
                    new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{connection.ContainerName}?{connection.SasToken}")),
                AuthMethod.OAuth => new BlobContainerClient(
                    new Uri($"https://{connection.StorageAccountName}.blob.core.windows.net/{connection.ContainerName}"),
                    new ClientSecretCredential(connection.OAuth.TenantId, connection.OAuth.ClientId, connection.OAuth.ClientSecret)),
                _ => throw new ArgumentOutOfRangeException(nameof(connection.AuthMethod), "Invalid auth method")
            };

            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            foreach (var partitionId in input.PartitionIds)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var blobName = $"{connection.EventHubNamespace}/{input.EventHubName}/{input.ConsumerGroup}/checkpoint/{partitionId}";
                    var blobClient = containerClient.GetBlobClient(blobName);

                    if (!await blobClient.ExistsAsync(cancellationToken))
                    {
                        if (options.FailIfPartitionMissing)
                            throw new Exception($"Checkpoint not found for partition {partitionId}");

                        skippedPartitions.Add(partitionId);
                        errorDetails.Add(new Error
                        {
                            Message = $"Checkpoint not found for partition {partitionId}",
                            AdditionalInfo = new Exception($"Partition ID: {partitionId} checkpoint does not exist."),
                        });
                        continue;
                    }

                    var blobContent = await blobClient.DownloadContentAsync(cancellationToken);
                    var json = JsonDocument.Parse(blobContent.Value.Content.ToString());

                    var properties = json.RootElement;

                    long offset = properties.GetProperty("offset").GetInt64();
                    long sequenceNumber = properties.GetProperty("sequenceNumber").GetInt64();
                    DateTimeOffset enqueuedTime = properties.GetProperty("enqueuedTimeUtc").GetDateTimeOffset();

                    if (input.RollbackEvents > 0)
                    {
                        sequenceNumber = Math.Max(0, sequenceNumber - input.RollbackEvents);
                        rollbackApplied = true;
                    }

                    if (options.TimestampAdjustment.HasValue)
                        enqueuedTime = enqueuedTime.Add(options.TimestampAdjustment.Value);

                    var newCheckpoint = new
                    {
                        offset,
                        sequenceNumber,
                        enqueuedTimeUtc = enqueuedTime.ToString("o"),
                    };

                    using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(newCheckpoint));
                    await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

                    updatedPartitions.Add(partitionId);
                }
                catch (Exception ex)
                {
                    skippedPartitions.Add(partitionId);
                    errorDetails.Add(new Error
                    {
                        Message = $"Failed to update checkpoint for partition {partitionId}: {ex.Message}",
                        AdditionalInfo = ex,
                    });
                }
            }

            if (errorDetails.Count > 0)
            {
                return ErrorHandler.Handle(
                    errorDetails,
                    options.ThrowErrorOnFailure,
                    "Failed to update one or more checkpoints.",
                    updatedPartitions,
                    skippedPartitions,
                    rollbackApplied);
            }

            return new Result
            {
                Success = true,
                UpdatedPartitions = updatedPartitions.ToArray(),
                SkippedPartitions = skippedPartitions.ToArray(),
                RollbackApplied = rollbackApplied,
                Errors = Array.Empty<Error>(),
            };
        }
        catch (Exception ex)
        {
            return ErrorHandler.Handle(
                ex,
                options.ThrowErrorOnFailure,
                options.ErrorMessageOnFailure);
        }
    }
}
