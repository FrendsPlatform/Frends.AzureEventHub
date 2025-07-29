using System;
using System.Collections.Generic;
using System.Linq;
using Frends.AzureEventHub.UpdateCheckpoint.Definitions;

namespace Frends.AzureEventHub.UpdateCheckpoint.Helpers;

/// <summary>
/// Handles error with usage of a standard ThrowOnFailure Frends flag
/// </summary>
public static class ErrorHandler
{
    /// <summary>
    /// Handler for exception
    /// </summary>
    /// <returns>Throw exception if a flag is true, else return Result with Error info</returns>
    public static Result Handle(Exception exception, bool throwOnFailure, string errorMessage)
    {
        if (throwOnFailure)
        {
            throw new Exception($"{errorMessage}\n{exception.Message}", exception);
        }

        return new Result
        {
            Success = false,
            UpdatedPartitions = Array.Empty<string>(),
            SkippedPartitions = Array.Empty<string>(),
            RollbackApplied = false,
            Errors = new[]
            {
                new Error
                {
                    Message = $"{errorMessage}\n{exception.Message}",
                    AdditionalInfo = exception,
                },
            },
        };
    }

    /// <summary>
    /// Handler for exceptions
    /// </summary>
    /// <returns>Throw exception if a flag is true, else return Result with Error info</returns>
    public static Result Handle(
            List<Error> errors,
            bool throwOnFailure,
            string errorMessage,
            List<string> updatedPartitions,
            List<string> skippedPartitions,
            bool rollbackApplied)
    {
        if (throwOnFailure)
        {
            var combinedMessage = $"{errorMessage}\n{string.Join("\n", errors.Select(e => e.Message))}";
            throw new Exception(combinedMessage, new AggregateException(errors.Select(e => e.AdditionalInfo as Exception)));
        }

        return new Result
        {
            Success = false,
            UpdatedPartitions = updatedPartitions.ToArray(),
            SkippedPartitions = skippedPartitions.ToArray(),
            RollbackApplied = rollbackApplied,
            Errors = errors.ToArray(),
        };
    }
}