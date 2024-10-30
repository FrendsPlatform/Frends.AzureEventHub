using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Frends.AzureEventHub.Receive.Definitions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Frends.AzureEventHub.Receive.Tests;

[TestFixture]
class Receive
{
    private static Consumer _consumer;
    private static Checkpoint _checkpoint;
    private static Options _options;
    private readonly string _storageAccount = "testsorage01";
    private static string _containerName;
    private readonly string _testFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "TestFile.xml");
    private readonly string _eventHubConnectionString = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTION_STRING");
    private readonly string _blobStorageConnectionString = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ConnString");
    private readonly string _appID = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_AppID");
    private readonly string _tenantID = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_TenantID");
    private readonly string _clientSecret = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_ClientSecret");
    private readonly string _accessKey = Environment.GetEnvironmentVariable("HiQ_AzureBlobStorage_testsorage01AccessKey");
    private readonly string _namespace = Environment.GetEnvironmentVariable("HIQ_AzureEventHub_FullyQualifiedNamespace");
    private readonly string _eventhubKey = Environment.GetEnvironmentVariable("HIQ_AzureEventHub_Key");

    [SetUp]
    public async Task SetUp()
    {
        _containerName = "eventcontainer" + Guid.NewGuid().ToString();

        _consumer = new Consumer
        {
            AuthenticationMethod = AuthenticationMethod.ConnectionString,
            ConnectionString = _eventHubConnectionString,
            EventHubName = "the-hub",
            ClientId = _appID,
            ClientSecret = _clientSecret,
            Namespace = _namespace,
            TenantId = _tenantID,
            MaximumWaitTime = 10,
            SASToken = default,
            ConsumerGroup = default,
        };

        _checkpoint = new Checkpoint()
        {
            AuthenticationMethod = AuthenticationMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            ClientId = _appID,
            ClientSecret = _clientSecret,
            TenantId = _tenantID,
            CreateContainer = true,
            SASToken = default,
            BlobContainerUri = default,
        };

        _options = new Options()
        {
            ExceptionHandler = ExceptionHandlers.Info,
            MaxEvents = 0,
            MaxRunTime = 10,
            ConsumeAttemptDelay = 1,
        };

        await GenerateEvent();
    }

    [TearDown]
    public async Task Teardown()
    {
        var blobServiceClient = new BlobServiceClient(_blobStorageConnectionString);
        var container = blobServiceClient.GetBlobContainerClient(_containerName);
        await container.DeleteIfExistsAsync(null);
    }

    [Test]
    public void ReceiveEvents_MaximumWaitTime_IsGreaterThan_MaxRunTime_Throw()
    {
        _options.MaxRunTime = 1;
        _consumer.MaximumWaitTime = 2;
        var result = Assert.ThrowsAsync<Exception>(async () => await AzureEventHub.Receive(_consumer, _checkpoint, _options, default));
        Assert.AreEqual("Consumer.MaximumWaitTime cannot exceed Options.MaxRunTime when Options.MaxRunTime is greater than 0.", result.Message);
    }

    [Test]
    public void ReceiveEvents_NoLimit_Throw()
    {
        _options.MaxRunTime = 0;
        _options.MaxEvents = 0;
        _options.ExceptionHandler = ExceptionHandlers.Throw;
        var result = Assert.ThrowsAsync<Exception>(async () => await AzureEventHub.Receive(_consumer, _checkpoint, _options, default));
        Assert.AreEqual("Both Options.MaxEvents and Options.MaxRunTime cannot be unlimited.", result.Message);
    }

    [Test]
    public void ReceiveEvents_MissingConnectionString_Throw()
    {
        _consumer.ConnectionString = "";
        _options.ExceptionHandler = ExceptionHandlers.Throw;
        var result = Assert.ThrowsAsync<ArgumentException>(async () => await AzureEventHub.Receive(_consumer, _checkpoint, _options, default));
        Assert.AreEqual("Value cannot be an empty string. (Parameter 'connectionString')", result.Message);
    }

    [Test]
    public async Task ReceiveEvents_MissingConnectionString_Info()
    {
        _consumer.ConnectionString = "";
        var result = await AzureEventHub.Receive(_consumer, _checkpoint, _options, default);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.Data.Count);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.IsTrue(result.Errors[0].Contains("An exception occurred: System.ArgumentException: Value cannot be an empty string"));
    }

    [Test]
    public void ReceiveEvents_CreateContainerFalse_Throw()
    {
        _checkpoint.CreateContainer = false;
        _options.ExceptionHandler = ExceptionHandlers.Throw;
        var result = Assert.ThrowsAsync<AggregateException>(async () => await AzureEventHub.Receive(_consumer, _checkpoint, _options, default));
        Assert.IsTrue(result.Message.ToString().Contains("The specified container does not exist"));
    }

    [Test]
    public async Task ReceiveEvents_CreateContainerFalse_Info()
    {
        _checkpoint.CreateContainer = false;
        var result = await AzureEventHub.Receive(_consumer, _checkpoint, _options, default);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.Data.Count);
        Assert.IsTrue(result.Errors.Count > 0);
        Assert.IsTrue(result.Errors.Any(element => element.Contains("The specified container does not exist")));
    }

    [Test]
    public async Task ReceiveEvents_ConnectionString_Success()
    {
        var result = await AzureEventHub.Receive(_consumer, _checkpoint, _options, default);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.Count > 0);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.IsTrue(result.Data.Any(element => element.Contains("Lorem")));
    }

    [Test]
    public async Task ReceiveEvents_SASToken_Success()
    {
        _consumer.AuthenticationMethod = AuthenticationMethod.SASToken;
        _consumer.SASToken = GenerateSASToken_Hub();
        var result = await AzureEventHub.Receive(_consumer, _checkpoint, _options, default);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.Count > 0);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.IsTrue(result.Data.Any(element => element.Contains("Lorem")));
    }

    [Test]
    public async Task ReceiveEvents_OAuth2_Success()
    {
        _checkpoint.AuthenticationMethod = AuthenticationMethod.OAuth2;
        _checkpoint.BlobContainerUri = CreateContainer();
        _options.ConsumeAttemptDelay = 1;
        var result = await AzureEventHub.Receive(_consumer, _checkpoint, _options, default);
        await Task.Delay(10000);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.IsTrue(result.Data.Count > 0);
        Assert.IsTrue(result.Data.Any(element => element.Contains("Lorem")));
    }

    private async Task GenerateEvent()
    {
        string inputString = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer sit amet justo eget nunc elementum ultrices. Duis vitae urna ut sem laoreet bibendum id eu nisi. Nullam iaculis vehicula nulla, sed suscipit ex. Vivamus iaculis, felis eget varius dignissim, justo nunc mattis felis, vel efficitur sapien turpis non est. ";

        var sentences = new List<string>();
        var delimiters = new[] { ".", "!", "?" };
        string[] tempSentences = inputString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

        foreach (string sentence in tempSentences)
            sentences.Add(sentence.Trim());

        var producerClient = new EventHubProducerClient(_eventHubConnectionString);

        using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

        foreach (var item in sentences)
        {
            var messageBytes = Encoding.UTF8.GetBytes(item);
            var eventData = new EventData(messageBytes);
            if (!eventBatch.TryAdd(eventData))
                throw new InvalidOperationException($"Event {item} is too large for the batch; "
                    + $"maximum batch size is {eventBatch.MaximumSizeInBytes} bytes, current batch size is {eventBatch.SizeInBytes} bytes and message size is {messageBytes.Length} bytes.");
        }

        try
        {
            await producerClient.SendAsync(eventBatch);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        finally
        {
            await producerClient.CloseAsync();
            await producerClient.DisposeAsync();
        }
    }

    private string CreateContainer()
    {
        try
        {
            BlobContainerClient blobContainerClient = new(_blobStorageConnectionString, _containerName);
            blobContainerClient.CreateIfNotExists(PublicAccessType.None, null, null);

            return blobContainerClient.Uri.ToString();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private string GenerateSASToken_Blob()
    {
        try
        {
            BlobServiceClient blobServiceClient = new(_blobStorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            containerClient.CreateIfNotExists();

            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = _containerName,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5),
            };
            sasBuilder.SetPermissions(BlobSasPermissions.All);

            return sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(_storageAccount, _accessKey)).ToString();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private string GenerateSASToken_Hub()
    {
        try
        {
            var resourceUri = @$"https://eh-task-development.servicebus.windows.net";

            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + 120); // 2 minutes expiration
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(_eventhubKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = $"SharedAccessSignature sr={HttpUtility.UrlEncode(resourceUri)}&sig={HttpUtility.UrlEncode(signature)}&se={expiry}&skn=Listen";
            return sasToken;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}