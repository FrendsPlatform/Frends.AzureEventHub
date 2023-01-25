using Frends.AzureEventHub.Send.Definitions;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.AzureEventHub.Send.Tests;

[TestFixture]
class Send
{
    private string _connectionString;
    private readonly Options _options = new Options();

    public Send()
    {
        _connectionString = Environment.GetEnvironmentVariable("EVENT_HUB_CONNECTION_STRING");
    }

    [Test]
    public async Task SendTest()
    {
        var input = new Input
        {
            ConnectionString = _connectionString,
            Messages = new EventHubMessage[]
            {
                new EventHubMessage { Message = "Hello, event hub 1!" },
                new EventHubMessage { Message = "Hello, event hub 2!" },
                new EventHubMessage { Message = "Hello, event hub 3!" },
            }
        };
        var result = await AzureEventHub.Send(input, _options, CancellationToken.None);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("A batch of 3 events has been published.", result.Message);
    }

    [Test]
    public void ExceedAllowedSize()
    {
        // create big message
        var sb = new StringBuilder();
        for(var i = 0; i < 10000; i++) { sb.Append("this is a string that is going to be long"); }
        var message = sb.ToString();
        var input = new Input
        {
            ConnectionString = _connectionString,
            Messages = new EventHubMessage[]
            {
                new EventHubMessage { Message = message },
            }
        };
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => AzureEventHub.Send(input, _options, CancellationToken.None));
        Assert.IsTrue(exception.Message.Contains("Event 0 is too large for the batch"));
    }

    [Test]
    public void InvalidConnectionStringFormat()
    {
        var input = new Input
        {
            ConnectionString = "This is not a valid connection string",
            Messages = new EventHubMessage[]
            {
                new EventHubMessage { Message = "nope" },
            }
        };

        var exception = Assert.ThrowsAsync<FormatException>(
            () => AzureEventHub.Send(input, _options, CancellationToken.None));
    }

    [Test]
    public void ConnectionStringBadKey()
    {
        var input = new Input
        {
            ConnectionString = "Endpoint=sb://eh-task-development.servicebus.windows.net/;SharedAccessKeyName=send;SharedAccessKey=thisisnotarealkey;EntityPath=the-hub",
            Messages = new EventHubMessage[]
            {
                new EventHubMessage { Message = "nope" },
            }
        };

        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => AzureEventHub.Send(input, _options, CancellationToken.None));
    }
}