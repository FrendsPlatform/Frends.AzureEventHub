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

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests;

[TestFixture]
internal class IntegrationTest
{
    private string _hubNamespace;
    private string _hubName;
    private string _consumer;
    private string _containerName;
    private string _eventHubConn;
    private string _blobConn;

    private BlobContainerClient _container;
    private Input _input;
    private Connection _connection;
    private Options _opts;
    private List<string> _resumedEvents = [];

    public void SetupEnvironment()
    {
        _eventHubConn = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTION_STRING");
        _blobConn = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_CONNSTRING");
        _hubNamespace = Environment.GetEnvironmentVariable("HIQ_AZUREEVENTHUB_FULLYQUALIFIEDNAMESPACE");
        _hubName = Helpers.ExtractEntityPath(_eventHubConn);
        _consumer = EventHubConsumerClient.DefaultConsumerGroupName;
        _containerName = "checkpoint-" + Guid.NewGuid().ToString("N");

        _connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobConn,
            ContainerName = _containerName,
            EventHubNamespace = _hubNamespace,
        };

        _input = new Input
        {
            EventHubName = _hubName,
            ConsumerGroup = _consumer,
            PartitionIds = ["0"],
            RollbackEvents = 1,
        };

        _opts = new Options { FailIfPartitionMissing = true };
    }

    [Test]
    public async Task UpdateCheckpoints_Integration_EventProcessorResumesFromUpdatedCheckpoint2()
    {
        // ARRANGE
        SetupEnvironment();
        await CreateContainer();
        await SendEventsToPartition("0", 5);

        var sequenceNumber = await RunProcessorUntilEventsRead(3);
        await CreateCheckpoint(sequenceNumber);

        // ACT
        await AzureEventHub.UpdateCheckpoint(_input, _connection, _opts, CancellationToken.None);

        // ASSERT – checkpoint value changed correctly
        var updated = await GetCheckpointSequence();
        Assert.That(updated, Is.EqualTo(sequenceNumber - 1));

        // ASSERT – processor resumes from updated checkpoint
        await RunProcessorAfterRollback();
        Assert.That(_resumedEvents.Count, Is.GreaterThan(0));

        await CleanupContainer();
    }

    private async Task CreateContainer()
    {
        _container = new BlobContainerClient(_blobConn, _containerName);
        await _container.CreateIfNotExistsAsync();
    }

    private async Task SendEventsToPartition(string partitionId, int count)
    {
        var producer = new EventHubProducerClient(_eventHubConn);
        var batch = await producer.CreateBatchAsync(new CreateBatchOptions { PartitionId = partitionId });

        for (int i = 0; i < count; i++)
            batch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"test-{i}")));

        await producer.SendAsync(batch);
        await producer.DisposeAsync();
    }

    private async Task<long> RunProcessorUntilEventsRead(int required)
    {
        var tcs = new TaskCompletionSource<bool>();
        var read = new List<EventData>();

        var processor = new EventProcessorClient(_container, _consumer, _eventHubConn);
        processor.ProcessEventAsync += args =>
        {
            read.Add(args.Data);
            if (read.Count >= required) tcs.TrySetResult(true);
            return Task.CompletedTask;
        };
        processor.ProcessErrorAsync += _ => Task.CompletedTask;

        await processor.StartProcessingAsync();
        await tcs.Task;
        await Task.Delay(1000);
        await processor.StopProcessingAsync();

        return read.Last().SequenceNumber;
    }

    private async Task CreateCheckpoint(long seq)
    {
        var blob = _container.GetBlobClient($"{_hubNamespace}/{_hubName}/{_consumer}/checkpoint/0");
        await blob.UploadAsync(new MemoryStream(), metadata: new Dictionary<string, string>
        {
            ["offset"] = "1000",
            ["sequencenumber"] = seq.ToString(),
        });
    }

    private async Task<long> GetCheckpointSequence()
    {
        var blob = _container.GetBlobClient($"{_hubNamespace}/{_hubName}/{_consumer}/checkpoint/0");
        var props = await blob.GetPropertiesAsync();
        return long.Parse(props.Value.Metadata["sequencenumber"]);
    }

    private async Task RunProcessorAfterRollback()
    {
        var tcs = new TaskCompletionSource<bool>();
        var p = new EventProcessorClient(_container, _consumer, _eventHubConn);

        p.ProcessEventAsync += args =>
        {
            var text = Encoding.UTF8.GetString(args.Data.EventBody.ToArray());
            _resumedEvents.Add(text);
            if (_resumedEvents.Count >= 2) tcs.TrySetResult(true);
            return Task.CompletedTask;
        };

        p.ProcessErrorAsync += _ => Task.CompletedTask;

        await p.StartProcessingAsync();

        var prod = new EventHubProducerClient(_eventHubConn);
        await prod.SendAsync(new[] { new EventData(Encoding.UTF8.GetBytes("trigger")) });

        await Task.WhenAny(tcs.Task, Task.Delay(15000));
        await p.StopProcessingAsync();
    }

    private async Task CleanupContainer() => await _container.DeleteAsync();
}
