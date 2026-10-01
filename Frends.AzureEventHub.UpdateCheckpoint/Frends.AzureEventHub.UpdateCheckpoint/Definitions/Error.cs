using System;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>Unable to join strings.</example>
    public string Message { get; set; }

    /// <summary>
    /// The original exception, or an AggregateException containing per-partition failures.
    /// </summary>
    /// <example>object { Exception Exception }</example>
    public Exception AdditionalInfo { get; set; }
}
