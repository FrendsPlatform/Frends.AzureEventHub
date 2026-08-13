using Frends.AzureEventHub.Receive.Definitions;
using Frends.AzureEventHub.Receive.Helpers;
using NUnit.Framework;
using System;
using System.Threading;

namespace Frends.AzureEventHub.Receive.Tests;

[TestFixture]
internal class ErrorHandlerTest
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    private static Options DefaultOptions() => new()
    {
        ThrowErrorOnFailure = true,
        ErrorMessageOnFailure = string.Empty,
        ExceptionHandler = ExceptionHandlers.Info,
        ConsumeAttemptDelay = 1,
        MaxRunTime = 10,
        MaxEvents = 1,
    };

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        var options = DefaultOptions();
        var ex = new Exception("test error");
        var thrown = Assert.Throws<Exception>(() => ex.Handle(options));
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown.Message, Is.EqualTo("test error"));
    }

    [Test]
    public void Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var ex = new Exception("test error");
        var result = ex.Handle(options);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.EqualTo("test error"));
        Assert.That(result.Error.AdditionalInfo, Is.SameAs(ex));
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;
        var ex = new Exception("original error");
        var thrown = Assert.Throws<Exception>(() => ex.Handle(options));
        Assert.That(thrown, Is.Not.Null);
        Assert.That(thrown.Message, Does.Contain(CustomErrorMessage));
    }

    [Test]
    public void Should_Rethrow_OperationCanceledException_When_ThrowCanceled_Is_True()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var ex = new OperationCanceledException("canceled");
        Assert.Throws<OperationCanceledException>(() => ex.Handle(options, throwCanceled: true));
    }

    [Test]
    public void Should_Return_Failed_Result_For_OperationCanceledException_When_ThrowCanceled_Is_False()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;
        var ex = new OperationCanceledException("canceled");
        var result = ex.Handle(options, throwCanceled: false);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
    }
}
