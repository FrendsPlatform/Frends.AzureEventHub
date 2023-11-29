using System.ComponentModel;

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
}