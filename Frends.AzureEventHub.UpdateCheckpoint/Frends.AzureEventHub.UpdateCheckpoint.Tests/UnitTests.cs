using System;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;
using Frends.AzureEventHub.UpdateCheckpoint.Helpers;
using NUnit.Framework;

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests;

/// <summary>
/// Validation-level tests that do not require live Azure resources. These exercise the
/// argument and authentication validation that runs before any network call is made.
/// End-to-end behavior (relative rollback, absolute targeting, range validation,
/// enqueued-time targeting, ownership) is covered in <see cref="IntegrationTest"/>.
/// </summary>
[TestFixture]
public class UnitTests
{
    [Test]
    public void UpdateCheckpoints_MissingEventHubName_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = string.Empty, ConsumerGroup = "$Default", Targets = [new PartitionTarget { PartitionId = "0" }] };
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, ValidStorageConnection(), options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("EventHubName is required"));
    }

    [Test]
    public void UpdateCheckpoints_MissingConsumerGroup_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = string.Empty, Targets = [new PartitionTarget { PartitionId = "0" }] };
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, ValidStorageConnection(), options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("ConsumerGroup is required"));
    }

    [Test]
    public void UpdateCheckpoints_NoPartitionsAndNoTargets_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default" };
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, ValidStorageConnection(), options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("At least one Target is required"));
    }

    [Test]
    public async Task UpdateCheckpoints_NoPartitionsAndNoTargets_ReturnsErrorWhenNotThrowing()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default" };
        var options = new Options { ThrowErrorOnFailure = false };

        var result = await AzureEventHub.UpdateCheckpoint(input, ValidStorageConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("At least one Target is required"));
        Assert.That(result.Error.AdditionalInfo, Is.Not.TypeOf<AggregateException>());
        Assert.That(result.Error.AdditionalInfo.Message, Does.Contain("At least one Target is required"));
    }

    [Test]
    public void UpdateCheckpoints_ResultContainsOnlyOneErrorProperty()
    {
        Assert.That(typeof(Result).GetProperty("Error"), Is.Not.Null);
        Assert.That(typeof(Result).GetProperty("Errors"), Is.Null);
    }

    [Test]
    public void AggregateFailure_ReturnsAggregateInError()
    {
        var first = new InvalidOperationException("Partition 0 failed");
        var second = new ArgumentException("Partition 1 failed");
        var aggregate = new AggregateException("Failed to update one or more checkpoints.", first, second);
        var result = aggregate.Handle(
            new Options { ThrowErrorOnFailure = false, ErrorMessageOnFailure = "Custom failure" },
            [],
            ["0", "1"],
            false,
            []);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("Custom failure"));
        Assert.That(result.Error.AdditionalInfo, Is.SameAs(aggregate));
        Assert.That(((AggregateException)result.Error.AdditionalInfo).InnerExceptions, Is.EqualTo(new Exception[] { first, second }));
    }

    [Test]
    public void AggregateFailure_ThrowsOriginalAggregateWhenConfigured()
    {
        var aggregate = new AggregateException("Failed to update one or more checkpoints.", new InvalidOperationException("Partition 0 failed"));

        var thrown = Assert.Throws<AggregateException>(() =>
            aggregate.Handle(new Options { ThrowErrorOnFailure = true }, [], ["0"], false, []));

        Assert.That(thrown, Is.SameAs(aggregate));
    }

    [Test]
    public void UpdateCheckpoints_StorageSasTokenMissing_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default", Targets = [new PartitionTarget { PartitionId = "0" }] };
        var connection = ValidStorageConnection();
        connection.AuthMethod = AuthMethod.SasToken;
        connection.SasToken = null;
        connection.StorageAccountName = "test";
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("SasToken must be provided when using SasToken auth method"));
    }

    [Test]
    public void UpdateCheckpoints_StorageOAuthMissing_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default", Targets = [new PartitionTarget { PartitionId = "0" }] };
        var connection = ValidStorageConnection();
        connection.AuthMethod = AuthMethod.OAuth;
        connection.OAuth = null;
        connection.StorageAccountName = "test";
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("OAuth configuration must be provided when using OAuth auth method"));
    }

    [Test]
    public void UpdateCheckpoints_EventHubConnectionStringMissing_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default", Targets = [new PartitionTarget { PartitionId = "0" }] };
        var connection = ValidStorageConnection();
        connection.EventHubAuthMethod = AuthMethod.ConnectionString;
        connection.EventHubConnectionString = null;
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("EventHubConnectionString must be provided"));
    }

    [Test]
    public void UpdateCheckpoints_EventHubOAuthMissing_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default", Targets = [new PartitionTarget { PartitionId = "0" }] };
        var connection = ValidStorageConnection();
        connection.EventHubAuthMethod = AuthMethod.OAuth;
        connection.OAuth = null;
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("OAuth configuration must be provided when using OAuth Event Hub auth method"));
    }

    [Test]
    public void UpdateCheckpoints_EventHubSasTokenMissing_ThrowsArgumentException()
    {
        var input = new Input { EventHubName = "hub", ConsumerGroup = "$Default", Targets = [new PartitionTarget { PartitionId = "0" }] };
        var connection = ValidStorageConnection();
        connection.EventHubAuthMethod = AuthMethod.SasToken;
        connection.EventHubSasToken = null;
        var options = new Options { ThrowErrorOnFailure = true };

        var ex = Assert.CatchAsync<Exception>(() =>
            AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("EventHubSasToken must be provided"));
    }

    private static Connection ValidStorageConnection() => new()
    {
        AuthMethod = AuthMethod.ConnectionString,
        ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
        ContainerName = "checkpoints",
        EventHubNamespace = "test.servicebus.windows.net",
        EventHubAuthMethod = AuthMethod.ConnectionString,
        EventHubConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
    };
}
