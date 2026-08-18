using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Options parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Determines how exceptions are handled during Task execution.
    /// Setting this to ExceptionHandlers.Info will log the exception message to Result.Errors and attempt to continue the Task execution, if possible.
    /// </summary>
    /// <example>ExceptionHandlers.Info</example>
    [DefaultValue(ExceptionHandlers.Info)]
    public ExceptionHandlers ExceptionHandler { get; set; }

    /// <summary>
    /// Specifies the delay (in seconds) between each attempt to consume data.
    /// NOTE: Must be at least 0.1
    /// </summary>
    /// <example>1, 0.5</example>
    [DefaultValue(1)]
    public double ConsumeAttemptDelay { get; set; }

    /// <summary>
    /// Sets the maximum duration (in seconds) for the Task to run.
    /// If set to 0, the Task can run indefinitely.
    /// Note that both MaxRunTime and MaxEvents cannot be set to unlimited.
    /// </summary>
    /// <example>0, 10, 1.5</example>
    [DefaultValue((double)0)]
    public double MaxRunTime { get; set; }

    /// <summary>
    /// Defines the maximum number of events to be received before ending the Task.
    /// If set to 0, the Task can receive an unlimited number of events.
    /// Note that both MaxRunTime and MaxEvents cannot be set to unlimited.
    /// </summary>
    /// <example>0, 5</example>
    [DefaultValue(0)]
    public int MaxEvents { get; set; }

    /// <summary>
    /// If true, an exception is thrown on failure. If false, the error is returned in Result.Error.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Optional custom error message to use when ThrowErrorOnFailure is true or when returning a failed Result.
    /// If left empty, the original exception message is used.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
