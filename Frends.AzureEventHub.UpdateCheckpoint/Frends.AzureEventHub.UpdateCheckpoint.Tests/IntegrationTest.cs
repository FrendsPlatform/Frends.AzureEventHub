using System;
using System.Collections.Generic;
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
    private readonly List<string> _resumedEvents = [];

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

    public void SetupEnvironment()
    {
        _eventHubConn = Environment.GetEnvironmentVariable("FRENDS__AZURE_EVENT_HUB__CONNECTION_STRING");
        _blobConn = Environment.GetEnvironmentVariable("FRENDS__AZURE_BLOB_STORAGE__CONNECTION_STRING");
        _hubNamespace = Environment.GetEnvironmentVariable("FRENDS__AZURE_EVENT_HUB__FULLY_QUALIFIED_NAMESPACE");
        _hubName = Helpers.ExtractEntityPath(_eventHubConn);
        _consumer = EventHubConsumerClient.DefaultConsumerGroupName;
        _containerName = "checkpoint-" + Guid.NewGuid().ToString("N");

        _connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobConn,
            ContainerName = _containerName,
            EventHubNamespace = _hubNamespace,
            EventHubAuthMethod = AuthMethod.ConnectionString,
            EventHubConnectionString = _eventHubConn,
        };

        _input = new Input
        {
            EventHubName = _hubName,
            ConsumerGroup = _consumer,
            PartitionIds = ["0"],
            RollbackEvents = 1,
        };

        _opts = new Options { FailIfPartitionMissing = true, FailIfPartitionOwned = false };
    }

    [Test]
    public async Task UpdateCheckpoints_Integration_EventProcessorResumesFromUpdatedCheckpoint()
    {
        SetupEnvironment();
        await CreateContainer();
        await SendEventsToPartition("0", 5);

        var sequenceNumber = await RunProcessor(3, false, false);

        // ACT – relative rollback by 1 event.
        var result = await AzureEventHub.UpdateCheckpoint(_input, _connection, _opts, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RollbackApplied, Is.True);
        Assert.That(result.AppliedTargets.Length, Is.EqualTo(1));
        Assert.That(result.AppliedTargets[0].NewSequenceNumber, Is.EqualTo(sequenceNumber - 1));

        var updated = await GetCheckpointSequence();
        Assert.That(updated, Is.EqualTo(sequenceNumber - 1));

        // ASSERT – processor resumes from updated checkpoint.
        await RunProcessor(2, true, true);
        Assert.That(_resumedEvents.Count, Is.GreaterThan(0));

        await CleanupContainer();
    }

    [Test]
    public async Task UpdateCheckpoints_Integration_AbsoluteSequenceTarget_SetsCheckpoint()
    {
        SetupEnvironment();
        await CreateContainer();
        await SendEventsToPartition("0", 5);

        var lastSequence = await RunProcessor(5, false, false);
        var targetSequence = lastSequence - 2;

        _input.Targets = [new PartitionTarget { PartitionId = "0", TargetSequenceNumber = targetSequence }];

        var result = await AzureEventHub.UpdateCheckpoint(_input, _connection, _opts, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AppliedTargets.Single().NewSequenceNumber, Is.EqualTo(targetSequence));
        Assert.That(await GetCheckpointSequence(), Is.EqualTo(targetSequence));

        await CleanupContainer();
    }

    [Test]
    public async Task UpdateCheckpoints_Integration_OutOfRangeSequence_ReturnsError()
    {
        SetupEnvironment();
        await CreateContainer();
        await SendEventsToPartition("0", 3);
        await RunProcessor(3, false, false);

        _input.Targets = [new PartitionTarget { PartitionId = "0", TargetSequenceNumber = long.MaxValue }];
        _opts.ThrowErrorOnFailure = false;

        var result = await AzureEventHub.UpdateCheckpoint(_input, _connection, _opts, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SkippedPartitions, Contains.Item("0"));
        Assert.That(result.Errors.Single().Message, Does.Contain("outside the valid range"));

        await CleanupContainer();
    }

    [Test]
    public async Task UpdateCheckpoints_Integration_InvalidPartition_ReturnsError()
    {
        SetupEnvironment();
        await CreateContainer();

        _input.Targets = [new PartitionTarget { PartitionId = "999", TargetSequenceNumber = 1 }];
        _opts.ThrowErrorOnFailure = false;

        var result = await AzureEventHub.UpdateCheckpoint(_input, _connection, _opts, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SkippedPartitions, Contains.Item("999"));
        Assert.That(result.Errors.Single().Message, Does.Contain("could not be resolved"));

        await CleanupContainer();
    }

    [Test]
    public async Task UpdateCheckpoints_Integration_EnqueuedTimeTarget_SetsCheckpoint()
    {
        SetupEnvironment();
        await CreateContainer();
        await SendEventsToPartition("0", 5);
        await RunProcessor(5, false, false);

        _input.Targets = [new PartitionTarget { PartitionId = "0", TargetEnqueuedTime = DateTimeOffset.UtcNow.AddMinutes(-30) }];

        var result = await AzureEventHub.UpdateCheckpoint(_input, _connection, _opts, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AppliedTargets.Single().PartitionId, Is.EqualTo("0"));

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

    private async Task<long> GetCheckpointSequence()
    {
        var blob = _container.GetBlobClient($"{_hubNamespace}/{_hubName}/{_consumer}/checkpoint/0");
        var props = await blob.GetPropertiesAsync();
        return long.Parse(props.Value.Metadata["sequencenumber"]);
    }

    private async Task<long> RunProcessor(
        int eventCountRequired,
        bool collectEventText,
        bool sendTriggerEvent)
    {
        var tcs = new TaskCompletionSource<bool>();
        var read = new List<EventData>();

        var processor = new EventProcessorClient(_container, _consumer, _eventHubConn);

        processor.ProcessEventAsync += async args =>
        {
            read.Add(args.Data);

            if (collectEventText)
            {
                var text = Encoding.UTF8.GetString(args.Data.EventBody.ToArray());
                _resumedEvents.Add(text);
            }

            await args.UpdateCheckpointAsync();

            if (read.Count >= eventCountRequired)
                tcs.TrySetResult(true);
        };

        processor.ProcessErrorAsync += _ => Task.CompletedTask;

        await processor.StartProcessingAsync();

        if (sendTriggerEvent)
        {
            var prod = new EventHubProducerClient(_eventHubConn);
            await prod.SendAsync(new[] { new EventData(Encoding.UTF8.GetBytes("trigger")) });
            await Task.WhenAny(tcs.Task, Task.Delay(15000));
        }
        else
        {
            await Task.WhenAny(tcs.Task, Task.Delay(15000));
            await Task.Delay(1000);
        }

        await processor.StopProcessingAsync();

        return read.Count > 0 ? read.Last().SequenceNumber : 0;
    }

    private async Task CleanupContainer() => await _container.DeleteAsync();
}
