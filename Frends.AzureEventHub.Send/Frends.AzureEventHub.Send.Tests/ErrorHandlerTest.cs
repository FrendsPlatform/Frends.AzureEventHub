using System;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureEventHub.Send.Definitions;
using NUnit.Framework;

namespace Frends.AzureEventHub.Send.Tests;

[TestFixture]
public class ErrorHandlerTest
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var ex = Assert.ThrowsAsync<Exception>(() =>
            AzureEventHub.Send(DefaultInput(), DefaultOptions(), CancellationToken.None));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var result = await AzureEventHub.Send(DefaultInput(), options, CancellationToken.None);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        var ex = Assert.ThrowsAsync<Exception>(() =>
            AzureEventHub.Send(DefaultInput(), options, CancellationToken.None));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain(CustomErrorMessage));
    }

    private static Input DefaultInput() => new()
    {
        ConnectionString = "Endpoint=sb://eh-task-development.servicebus.windows.net/;SharedAccessKeyName=send;SharedAccessKey=thisisnotarealkey;EntityPath=the-hub",
        Messages = new EventHubMessage[] { new() { Message = "test" } }
    };

    private static Options DefaultOptions() =>
        new() { ThrowErrorOnFailure = true, ErrorMessageOnFailure = string.Empty };
}
