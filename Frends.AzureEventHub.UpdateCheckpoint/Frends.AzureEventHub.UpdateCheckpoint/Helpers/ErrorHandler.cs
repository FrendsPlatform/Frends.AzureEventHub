using System;
using System.Runtime.ExceptionServices;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;

namespace Frends.AzureEventHub.UpdateCheckpoint.Helpers;

/// <summary>
/// Handles error with usage of a standard ThrowOnFailure Frends flag
/// </summary>
public static class ErrorHandler
{
    /// <summary>
    /// Handler for exceptions.
    /// </summary>
    /// <returns>Throws exception if canceled, or if options.ThrowErrorOnFailure is true, else returns Result with Error info.</returns>
    public static Result Handle(
        this Exception exception,
        Options options,
        string[] updatedPartitions,
        string[] skippedPartitions,
        bool rollbackApplied,
        AppliedTarget[] appliedTargets,
        bool throwCanceled = true)
    {
        ThrowIfCanceled(exception, throwCanceled);
        if (options.ThrowErrorOnFailure) ThrowBaseException(exception, options.ErrorMessageOnFailure);

        return ReturnResult(exception, options.ErrorMessageOnFailure, updatedPartitions, skippedPartitions, rollbackApplied, appliedTargets);
    }

    private static void ThrowIfCanceled(Exception exception, bool throwCanceled = true)
    {
        if (throwCanceled && exception is OperationCanceledException) ExceptionDispatchInfo.Capture(exception).Throw();
    }

    private static void ThrowBaseException(Exception exception, string customMessage = null)
    {
        if (string.IsNullOrEmpty(customMessage))
            ExceptionDispatchInfo.Capture(exception).Throw();

        throw new Exception(customMessage, exception);
    }

    private static Result ReturnResult(
        Exception exception,
        string customMessage,
        string[] updatedPartitions,
        string[] skippedPartitions,
        bool rollbackApplied,
        AppliedTarget[] appliedTargets)
    {
        var errorMessage = string.IsNullOrEmpty(customMessage)
            ? exception.Message
            : $"{customMessage}: {exception.Message}";

        return new Result
        {
            Success = false,
            UpdatedPartitions = updatedPartitions,
            SkippedPartitions = skippedPartitions,
            RollbackApplied = rollbackApplied,
            AppliedTargets = appliedTargets,
            Errors = new[]
            {
                new Error
                {
                    Message = errorMessage,
                    AdditionalInfo = exception,
                },
            },
        };
    }
}
