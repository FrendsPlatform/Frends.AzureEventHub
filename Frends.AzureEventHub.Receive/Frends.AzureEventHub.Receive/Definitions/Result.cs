using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Receive result.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the Task completed without errors.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; private set; }

    /// <summary>
    /// Contains a list of events.
    /// </summary>
    /// <example>{ "foo", bar }</example>
    public List<dynamic> Data { get; private set; }

    /// <summary>
    /// Contains a list of errors if Options.ExceptionHandler is set to Info.
    /// </summary>
    /// <example>{ "An exception occured", "Another exception occured" }</example>
    public List<dynamic> Errors { get; private set; }

    /// <summary>
    /// Contains error details when the Task fails and Options.ThrowErrorOnFailure is false.
    /// </summary>
    /// <example>null</example>
    public Error Error { get; internal set; }

    internal Result(bool success, ConcurrentBag<dynamic> data, ConcurrentBag<dynamic> errors)
    {
        Success = success;
        Data = data.ToList();
        Errors = errors.ToList();
    }
}

/// <summary>
/// Error details.
/// </summary>
public class Error
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; internal set; }

    /// <summary>
    /// Additional information about the error (the original exception).
    /// </summary>
    public Exception AdditionalInfo { get; internal set; }
}
