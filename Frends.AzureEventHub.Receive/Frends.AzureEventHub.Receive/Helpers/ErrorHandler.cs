using System;
using Frends.AzureEventHub.Receive.Definitions;

namespace Frends.AzureEventHub.Receive.Helpers;

internal static class ErrorHandler
{
    /// <summary>
    /// Handles the exception according to the task options.
    /// When ThrowErrorOnFailure is true, the exception is (re)thrown.
    /// When ThrowErrorOnFailure is false, a failed Result is returned.
    /// </summary>
    internal static Result Handle(this Exception exception, Options options, bool throwCanceled = true)
    {
        ThrowIfCanceled(exception, throwCanceled);
        if (options.ThrowErrorOnFailure) ThrowBaseException(exception, options.ErrorMessageOnFailure);

        return ReturnResult(exception, options.ErrorMessageOnFailure);
    }

    private static void ThrowIfCanceled(Exception exception, bool throwCanceled = true)
    {
        if (throwCanceled && exception is OperationCanceledException) throw exception;
    }

    private static void ThrowBaseException(Exception exception, string customMessage = null)
    {
        if (string.IsNullOrEmpty(customMessage))
            throw new Exception(exception.Message, exception);

        throw new Exception(customMessage, exception);
    }

    private static Result ReturnResult(Exception exception, string customMessage = null)
    {
        var errorMessage = string.IsNullOrEmpty(customMessage)
            ? exception.Message
            : $"{customMessage}: {exception.Message}";

        return new Result(false, new System.Collections.Concurrent.ConcurrentBag<dynamic>(), new System.Collections.Concurrent.ConcurrentBag<dynamic>())
        {
            Error = new Error
            {
                Message = errorMessage,
                AdditionalInfo = exception,
            },
        };
    }
}
