using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;
using NUnit.Framework;

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests;

[TestFixture]
public class UpdateCheckpointsTests
{
    private string _blobStorageConnectionString;
    private string _containerName;
    private string _eventHubNamespace;
    private string _eventHubName;
    private string _consumerGroup;
    private BlobContainerClient _containerClient;

    private string _storageAccountName;
    private string _sasToken;
    private string _tenantId;
    private string _clientId;
    private string _clientSecret;

    private Dictionary<string, DateTimeOffset> _originalTimestamps = new Dictionary<string, DateTimeOffset>();

    [SetUp]
    public async Task SetUp()
    {
        _blobStorageConnectionString = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_CONNSTRING");
        _containerName = "checkpointcontainer" + Guid.NewGuid().ToString();
        _eventHubNamespace = Environment.GetEnvironmentVariable("HIQ_AZUREEVENTHUB_FULLYQUALIFIEDNAMESPACE");
        var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTION_STRING");
        _eventHubName = ExtractEntityPathFromConnectionString(eventHubConnectionString);
        _consumerGroup = "$Default";
        _sasToken = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_TESTSORAGE01ACCESSKEY");
        _tenantId = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_TENANTID");
        _clientId = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_APPID");
        _clientSecret = Environment.GetEnvironmentVariable("HIQ_AZUREBLOBSTORAGE_CLIENTSECRET");
        _storageAccountName = ExtractStorageAccountName(_blobStorageConnectionString);
        _containerClient = new BlobContainerClient(_blobStorageConnectionString, _containerName);
        await _containerClient.CreateIfNotExistsAsync();

        _originalTimestamps.Clear();

        await CreateTestCheckpoints();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _containerClient.DeleteIfExistsAsync();
    }

    [Test]
    public async Task UpdateCheckpoints_MixedScenario_ReturnsPartialSuccess()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0", "1", "2", "3"],
            RollbackEvents = 5,
        };

        var connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };

        var options = new Options
        {
            FailIfPartitionMissing = false,
            ThrowErrorOnFailure = false,
            TimestampAdjustment = TimeSpan.FromMinutes(5),
        };

        var result = await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.UpdatedPartitions.Length, Is.EqualTo(2));
        Assert.That(result.SkippedPartitions.Length, Is.EqualTo(2));
        Assert.That(result.RollbackApplied, Is.True);
        Assert.That(result.Errors.Length, Is.EqualTo(2));

        Assert.That(result.UpdatedPartitions, Contains.Item("0"));
        Assert.That(result.UpdatedPartitions, Contains.Item("1"));

        Assert.That(result.SkippedPartitions, Contains.Item("2"));
        Assert.That(result.SkippedPartitions, Contains.Item("3"));

        await VerifyCheckpointRollback("0", 5);
        await VerifyCheckpointRollback("1", 5);
    }

    [Test]
    public async Task UpdateCheckpoints_AllPartitionsExist_ReturnsFullSuccess()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0", "1"],
            RollbackEvents = 0,
        };

        var connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };

        var options = new Options
        {
            FailIfPartitionMissing = false,
            ThrowErrorOnFailure = false,
        };

        var result = await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UpdatedPartitions.Length, Is.EqualTo(2));
        Assert.That(result.SkippedPartitions.Length, Is.EqualTo(0));
        Assert.That(result.RollbackApplied, Is.False);
        Assert.That(result.Errors.Length, Is.EqualTo(0));
    }

    [Test]
    public async Task UpdateCheckpoints_MixedScenario_WithThrowOnFailure_ThrowsException()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0", "1", "2", "3"],
            RollbackEvents = 5,
        };

        var connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };

        var options = new Options
        {
            FailIfPartitionMissing = false,
            ThrowErrorOnFailure = true,
            TimestampAdjustment = TimeSpan.FromMinutes(5),
        };

        try
        {
            await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Contains.Substring("Failed to update one or more checkpoints"));
        }
    }

    [Test]
    public void UpdateCheckpoints_InvalidInput_ThrowsArgumentException()
    {
        var input = new Input
        {
            EventHubName = string.Empty,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0"],
        };

        var connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };

        var options = new Options();

        Assert.ThrowsAsync<Exception>(() =>
                    AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None));
    }

    [Test]
    public async Task UpdateCheckpoints_FailIfPartitionMissing_ThrowsException()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0", "999"],
            RollbackEvents = 0,
        };

        var connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };

        var options = new Options
        {
            FailIfPartitionMissing = true,
            ThrowErrorOnFailure = true,
        };

        try
        {
            await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Contains.Substring("Checkpoint not found for partition 999"));
        }
    }

    [Test]
    public async Task UpdateCheckpoints_WithTimestampAdjustment_UpdatesTimestamp()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0"],
            RollbackEvents = 0,
        };

        var connection = new Connection
        {
            AuthMethod = AuthMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };

        var timestampAdjustment = TimeSpan.FromHours(2);
        var options = new Options
        {
            TimestampAdjustment = timestampAdjustment,
            ThrowErrorOnFailure = false,
        };

        var result = await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        await VerifyTimestampAdjustment("0", timestampAdjustment);
    }

    [Test]
    [Ignore("SAS token test - requires valid SAS token in HIQ_AZUREBLOBSTORAGE_TESTSORAGE01ACCESSKEY. Can be run locally with proper credentials.")]
    public async Task UpdateCheckpoints_SasToken_ReturnsSuccess()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0", "1"],
            RollbackEvents = 0,
        };
        var connection = new Connection
        {
            AuthMethod = AuthMethod.SasToken,
            SasToken = _sasToken,
            StorageAccountName = _storageAccountName,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };
        var options = new Options
        {
            FailIfPartitionMissing = false,
            ThrowErrorOnFailure = false,
        };

        var result = await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UpdatedPartitions, Contains.Item("0"));
        Assert.That(result.UpdatedPartitions, Contains.Item("1"));
    }

    [Test]
    [Ignore("OAuth test - requires valid OAuth credentials in HIQ_AZUREBLOBSTORAGE_TENANTID, HIQ_AZUREBLOBSTORAGE_APPID, and HIQ_AZUREBLOBSTORAGE_CLIENTSECRET. Can be run locally with proper credentials.")]
    public async Task UpdateCheckpoints_OAuth_ReturnsSuccess()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0", "1"],
            RollbackEvents = 0,
        };
        var connection = new Connection
        {
            AuthMethod = AuthMethod.OAuth,
            StorageAccountName = _storageAccountName,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
            OAuth = new OAuthConfig
            {
                TenantId = _tenantId,
                ClientId = _clientId,
                ClientSecret = _clientSecret,
            },
        };
        var options = new Options
        {
            FailIfPartitionMissing = false,
            ThrowErrorOnFailure = false,
        };

        var result = await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UpdatedPartitions, Contains.Item("0"));
        Assert.That(result.UpdatedPartitions, Contains.Item("1"));
    }

    [Test]
    public async Task UpdateCheckpoints_SasToken_MissingToken_ThrowsException()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0"],
            RollbackEvents = 0,
        };
        var connection = new Connection
        {
            AuthMethod = AuthMethod.SasToken,
            SasToken = null,
            StorageAccountName = _storageAccountName,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };
        var options = new Options
        {
            ThrowErrorOnFailure = true,
        };

        try
        {
            await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Contains.Substring("SasToken must be provided when using SasToken auth method"));
        }
    }

    [Test]
    public async Task UpdateCheckpoints_OAuth_MissingConfig_ThrowsException()
    {
        var input = new Input
        {
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionIds = ["0"],
            RollbackEvents = 0,
        };
        var connection = new Connection
        {
            AuthMethod = AuthMethod.OAuth,
            OAuth = null,
            StorageAccountName = _storageAccountName,
            ContainerName = _containerName,
            EventHubNamespace = _eventHubNamespace,
        };
        var options = new Options
        {
            ThrowErrorOnFailure = true,
        };

        try
        {
            await AzureEventHub.UpdateCheckpoint(input, connection, options, CancellationToken.None);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Contains.Substring("OAuth configuration must be provided when using OAuth auth method"));
        }
    }

    private string ExtractStorageAccountName(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("AccountName=".Length);
            }
        }

        return null;
    }

    private string ExtractEntityPathFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("EntityPath=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("EntityPath=".Length);
            }
        }

        return null;
    }

    private async Task CreateTestCheckpoints()
    {
        var partition0Timestamp = DateTimeOffset.UtcNow.AddHours(-1);
        _originalTimestamps["0"] = partition0Timestamp;
        await CreateCheckpoint("0", 1000, 100, partition0Timestamp);
        await CreateCheckpoint("1", 2000, 200, DateTimeOffset.UtcNow.AddHours(-2));
    }

    private async Task CreateCheckpoint(string partitionId, long offset, long sequenceNumber, DateTimeOffset enqueuedTime)
    {
        var blobName = $"{_eventHubNamespace}/{_eventHubName}/{_consumerGroup}/checkpoint/{partitionId}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var checkpoint = new
        {
            offset,
            sequenceNumber,
            enqueuedTimeUtc = enqueuedTime.ToString("o"),
        };

        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(checkpoint));
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    private async Task VerifyCheckpointRollback(string partitionId, int rollbackEvents)
    {
        var blobName = $"{_eventHubNamespace}/{_eventHubName}/{_consumerGroup}/checkpoint/{partitionId}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var blobContent = await blobClient.DownloadContentAsync();
        var json = JsonDocument.Parse(blobContent.Value.Content.ToString());
        var properties = json.RootElement;

        long sequenceNumber = properties.GetProperty("sequenceNumber").GetInt64();

        // Original sequence numbers were 100 and 200, so after rollback of 5:
        if (partitionId == "0")
            Assert.That(sequenceNumber, Is.EqualTo(100 - rollbackEvents));
        else if (partitionId == "1")
            Assert.That(sequenceNumber, Is.EqualTo(200 - rollbackEvents));
    }

    private async Task VerifyTimestampAdjustment(string partitionId, TimeSpan expectedAdjustment)
    {
        var blobName = $"{_eventHubNamespace}/{_eventHubName}/{_consumerGroup}/checkpoint/{partitionId}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadContentAsync();
        var checkpointJson = response.Value.Content.ToString();
        var checkpoint = JsonSerializer.Deserialize<JsonElement>(checkpointJson);

        var storedTimestampString = checkpoint.GetProperty("enqueuedTimeUtc").GetString();
        var storedTimestamp = DateTimeOffset.Parse(storedTimestampString);

        var originalTimestamp = _originalTimestamps[partitionId];
        var expectedTimestamp = originalTimestamp.Add(expectedAdjustment);

        Assert.That(storedTimestamp, Is.EqualTo(expectedTimestamp).Within(TimeSpan.FromMilliseconds(1)));
    }
}