using System;

namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// Error information returned when the Task fails and ThrowErrorOnFailure is set to false.
/// </summary>
public class Error
{
    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    /// <example>An error occurred while publishing events.</example>
    public string Message { get; set; }

    /// <summary>
    /// The exception that caused the failure.
    /// </summary>
    /// <example>System.Exception</example>
    public Exception AdditionalInfo { get; set; }
}
