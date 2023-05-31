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
using System.Globalization;
using System.IO;
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
    private readonly string _fullyQualifiedNamespace = Environment.GetEnvironmentVariable("HIQ_AzureEventHub_FullyQualifiedNamespace");
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
            ClientId = default,
            SASToken = default,
            ClientSecret = default,
            ConsumerGroup = default,
            FullyQualifiedNamespace = default,
            TenantId = default,
            MaximumWaitTime = 10
        };

        _checkpoint = new Checkpoint()
        {
            AuthenticationMethod = AuthenticationMethod.ConnectionString,
            ConnectionString = _blobStorageConnectionString,
            ContainerName = _containerName,
            CreateContainer = true,
            ClientId = default,
            SASToken = default,
            ClientSecret = default,
            TenantId = default,
            BlobContainerUri = default,
        };

        _options = new Options()
        {
            ExceptionHandler = ExceptionHandlers.Info,
            MaxEvents = 0,
            MaximumWaitTimeInMinutes = 0,
            Delay = 1,
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
    public void ReceiveEvents_CheckpointCS_MissingCS()
    {
        var consumer = _consumer;
        consumer.ConnectionString = "";
        var checkpoint = _checkpoint;
        var options = _options;
        options.MaximumWaitTimeInMinutes = 0.5;
        options.Delay = 1;

        Assert.Throws<NullReferenceException>(async () => await AzureEventHub.Receive(consumer, checkpoint, options, default));
    }

    [Test]
    public async Task ReceiveEvents_CheckpointCS_MaxWaitTime()
    {
        var consumer = _consumer;
        var checkpoint = _checkpoint;
        var options = _options;
        options.MaximumWaitTimeInMinutes = 0.5;
        options.Delay = 1;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.Count > 0); // There are multiple events to consume but can't be sure how many events will be consumed in given limit.
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
    }

    [Test]
    public async Task ReceiveEvents_ConsumerSAS_MaxEvent()
    {
        var consumer = _consumer;
        consumer.AuthenticationMethod = AuthenticationMethod.SASToken;
        consumer.SASToken = GenerateSASToken_Hub();
        consumer.FullyQualifiedNamespace = _fullyQualifiedNamespace;

        var checkpoint = _checkpoint;
        var options = _options;
        options.MaxEvents = 2;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Data.Count);
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
    }

    [Test]
    public void ReceiveEvents_ConsumerOA_MaxEvent_Throw()
    {
        var consumer = _consumer;
        consumer.AuthenticationMethod = AuthenticationMethod.OAuth2;
        consumer.TenantId = _tenantID;
        consumer.ClientId = _appID;
        consumer.ClientSecret = _clientSecret;
        consumer.FullyQualifiedNamespace = _fullyQualifiedNamespace;

        var checkpoint = _checkpoint;
        var options = _options;
        options.MaxEvents = 2;
        options.ExceptionHandler = ExceptionHandlers.Throw;

        // It seems to work as intended, but since there are no rights to consume any data, an exception is being asserted.
        Assert.ThrowsAsync<Exception>(async () => await AzureEventHub.Receive(consumer, checkpoint, options, default));
    }

    [Test]
    public async Task ReceiveEvents_ConsumerOA_MaxEvent_Info()
    {
        var consumer = _consumer;
        consumer.AuthenticationMethod = AuthenticationMethod.OAuth2;
        consumer.TenantId = _tenantID;
        consumer.ClientId = _appID;
        consumer.ClientSecret = _clientSecret;
        consumer.FullyQualifiedNamespace = _fullyQualifiedNamespace;

        var checkpoint = _checkpoint;
        var options = _options;
        options.MaxEvents = 2;
        options.ExceptionHandler = ExceptionHandlers.Info;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, result.Data.Count);
        Assert.IsTrue(result.Data[0].Contains("An exception occured: "));
    }

    [Test]
    public void ReceiveEvents_ConsumerOA_CreateContainerFalse_Throw()
    {
        var consumer = _consumer;
        consumer.AuthenticationMethod = AuthenticationMethod.SASToken;
        consumer.SASToken = GenerateSASToken_Hub();
        consumer.FullyQualifiedNamespace = _fullyQualifiedNamespace;

        var checkpoint = _checkpoint;
        checkpoint.CreateContainer = false;

        var options = _options;
        options.MaxEvents = 2;
        options.ExceptionHandler = ExceptionHandlers.Throw;

        Assert.ThrowsAsync<Exception>(async () => await AzureEventHub.Receive(consumer, checkpoint, options, default));
    }

    [Test]
    public async Task ReceiveEvents_CreateContainerFalse_Info()
    {
        var consumer = _consumer;
        consumer.AuthenticationMethod = AuthenticationMethod.SASToken;
        consumer.SASToken = GenerateSASToken_Hub();
        consumer.FullyQualifiedNamespace = _fullyQualifiedNamespace;

        var checkpoint = _checkpoint;
        checkpoint.CreateContainer = false;

        var options = _options;
        options.MaxEvents = 2;
        options.ExceptionHandler = ExceptionHandlers.Info;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Data[2].Contains("An exception occured"));
    }

    [Test]
    public async Task ReceiveEvents_CheckpointCS_MaxEvents()
    {
        var consumer = _consumer;
        var checkpoint = _checkpoint;
        var options = _options;
        options.MaxEvents = 2;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Data.Count);
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
    }

    [Test]
    public async Task ReceiveEvents_CheckpoinSAS_MaxWaitTime()
    {
        var consumer = _consumer;

        var checkpoint = _checkpoint;
        checkpoint.AuthenticationMethod = AuthenticationMethod.SASToken;
        checkpoint.SASToken = GenerateSASToken_Blob();
        checkpoint.BlobContainerUri = CreateContainer();

        var options = _options;
        options.MaximumWaitTimeInMinutes = 0.5;
        options.Delay = 1;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.Count > 0); // There are multiple events to consume but can't be sure how many events will be consumed in given limit.
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
    }

    [Test]
    public async Task ReceiveEvents_CheckpoinSAS_MaxEvents()
    {
        var consumer = _consumer;

        var checkpoint = _checkpoint;
        checkpoint.AuthenticationMethod = AuthenticationMethod.SASToken;
        checkpoint.SASToken = GenerateSASToken_Blob();
        checkpoint.BlobContainerUri = CreateContainer();

        var options = _options;
        options.MaxEvents = 2;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Data.Count);
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
    }

    [Test]
    public async Task ReceiveEvents_CheckpoinOA_MaxWaitTime()
    {
        var consumer = _consumer;

        var checkpoint = _checkpoint;
        checkpoint.AuthenticationMethod = AuthenticationMethod.OAuth2;
        checkpoint.BlobContainerUri = CreateContainer();
        checkpoint.TenantId = _tenantID;
        checkpoint.ClientId = _appID;
        checkpoint.ClientSecret = _clientSecret;

        var options = _options;
        options.MaximumWaitTimeInMinutes = 0.5;
        options.Delay = 1;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data.Count > 0); // There are multiple events to consume but can't be sure how many events will be consumed in given limit.
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
    }

    [Test]
    public async Task ReceiveEvents_CheckpoinOA_MaxEvents()
    {
        var consumer = _consumer;
        var checkpoint = _checkpoint;
        checkpoint.AuthenticationMethod = AuthenticationMethod.OAuth2;
        checkpoint.BlobContainerUri = CreateContainer();
        checkpoint.TenantId = _tenantID;
        checkpoint.ClientId = _appID;
        checkpoint.ClientSecret = _clientSecret;
        var options = _options;
        options.MaxEvents = 2;

        var result = await AzureEventHub.Receive(consumer, checkpoint, options, default);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.Data.Count);
        Assert.IsTrue(result.Data[0].Contains("Lorem"));
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
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
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
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
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
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, "Listen");
            return sasToken;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}