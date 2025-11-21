using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;
using NUnit.Framework;

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests
{
    [TestFixture]
    internal class IntegrationTest
    {
        [Test]
        public async Task UpdateCheckpoints_Integration_EventProcessorResumesFromUpdatedCheckpoint()
        {
            string eventHubConn = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTION_STRING");
            string blobConn = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_CONNSTRING");
            string containerName = "checkpoint-" + Guid.NewGuid().ToString("N");

            var consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
            var eventHubName = Helpers.ExtractEntityPathFromConnectionString(eventHubConn);
            var eventHubNamespace = Environment.GetEnvironmentVariable("HIQ_AZUREEVENTHUB_FULLYQUALIFIEDNAMESPACE");

            var blobContainerClient = new BlobContainerClient(blobConn, containerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            // Process real events
            var processedEvents = new List<(string Partition, long Sequence)>();
            var eventsProcessed = new TaskCompletionSource<bool>();

            var processor = new EventProcessorClient(blobContainerClient, consumerGroup, eventHubConn);

            processor.ProcessEventAsync += args =>
            {
                var partitionId = args.Partition.PartitionId;
                var sequence = args.Data.SequenceNumber;

                processedEvents.Add((partitionId, sequence));

                if (processedEvents.Count >= 3)
                {
                    eventsProcessed.TrySetResult(true);
                }

                return Task.CompletedTask;
            };

            processor.ProcessErrorAsync += args => Task.CompletedTask;

            await processor.StartProcessingAsync();

            // Send events to specific partition
            var producer = new EventHubProducerClient(eventHubConn);
            var batchOptions = new CreateBatchOptions { PartitionId = "0" };
            using var batch = await producer.CreateBatchAsync(batchOptions);
            for (int i = 0; i < 5; i++)
                batch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"test-{i}")));
            await producer.SendAsync(batch);
            await producer.DisposeAsync();

            await eventsProcessed.Task;
            await Task.Delay(2000);
            await processor.StopProcessingAsync();

            // Get sequence number from actual processed events
            var partition0Events = processedEvents.Where(e => e.Partition == "0").ToList();
            if (partition0Events.Count == 0)
            {
                await blobContainerClient.DeleteAsync();
                Assert.Fail("No events processed on partition 0");
                return;
            }

            var realSequenceNumber = partition0Events[2].Sequence;

            // Create checkpoint
            await CreateCheckpoint(blobContainerClient, eventHubNamespace, eventHubName, consumerGroup, "0", realSequenceNumber);

            // Verify checkpoint was created
            var blobName = $"{eventHubNamespace}/{eventHubName}/{consumerGroup}/checkpoint/0";
            var checkpointBlob = blobContainerClient.GetBlobClient(blobName);

            if (!await checkpointBlob.ExistsAsync())
            {
                await blobContainerClient.DeleteAsync();
                Assert.Fail("Checkpoint creation failed");
                return;
            }

            var properties = await checkpointBlob.GetPropertiesAsync();

            var connection = new Connection
            {
                AuthMethod = AuthMethod.ConnectionString,
                ConnectionString = blobConn,
                ContainerName = containerName,
                EventHubNamespace = eventHubNamespace,
            };

            var input = new Input
            {
                EventHubName = eventHubName,
                ConsumerGroup = consumerGroup,
                PartitionIds = ["0"],
                RollbackEvents = 1,
            };

            var opts = new Options { FailIfPartitionMissing = true };

            await AzureEventHub.UpdateCheckpoint(input, connection, opts, CancellationToken.None);

            // Verify the rollback worked
            var updatedProperties = await checkpointBlob.GetPropertiesAsync();
            var updatedSequence = long.Parse(updatedProperties.Value.Metadata["sequencenumber"]);

            Assert.That(updatedSequence, Is.EqualTo(realSequenceNumber - 1));

            // Test Event Processor behavior with the modified checkpoint
            var eventsAfterRollback = new List<string>();
            var resumedCompletion = new TaskCompletionSource<bool>();

            var processor2 = new EventProcessorClient(blobContainerClient, consumerGroup, eventHubConn);

            processor2.ProcessEventAsync += args =>
            {
                var body = Encoding.UTF8.GetString(args.Data.EventBody.ToArray());
                var sequence = args.Data.SequenceNumber;
                eventsAfterRollback.Add($"{body} (seq:{sequence})");

                if (eventsAfterRollback.Count >= 2)
                    resumedCompletion.TrySetResult(true);

                return Task.CompletedTask;
            };

            processor2.ProcessErrorAsync += args => Task.CompletedTask;

            await processor2.StartProcessingAsync();

            // Send trigger events
            var producer2 = new EventHubProducerClient(eventHubConn);
            await producer2.SendAsync(new[] { new EventData(Encoding.UTF8.GetBytes("trigger-1")) });
            await producer2.SendAsync(new[] { new EventData(Encoding.UTF8.GetBytes("trigger-2")) });
            await producer2.DisposeAsync();

            var timeout = Task.Delay(TimeSpan.FromSeconds(15));
            var completed = await Task.WhenAny(resumedCompletion.Task, timeout);

            await processor2.StopProcessingAsync();

            Assert.That(eventsAfterRollback.Count, Is.GreaterThan(0));

            await blobContainerClient.DeleteAsync();
        }

        private async Task CreateCheckpoint(BlobContainerClient container, string ns, string hub, string consumerGroup, string partitionId, long sequenceNumber)
        {
            var blobName = $"{ns}/{hub}/{consumerGroup}/checkpoint/{partitionId}";
            var blobClient = container.GetBlobClient(blobName);

            var metadata = new Dictionary<string, string>
            {
                ["offset"] = "274877907824",
                ["sequencenumber"] = sequenceNumber.ToString(),
                ["clientidentifier"] = Guid.NewGuid().ToString(),
            };

            using var emptyStream = new MemoryStream();
            await blobClient.UploadAsync(emptyStream, metadata: metadata);
        }
    }
}
