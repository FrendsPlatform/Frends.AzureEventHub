using System.ComponentModel;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Options parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// How exception will be handled.
    /// </summary>
    /// <example>ExceptionHandlers.Info</example>
    [DefaultValue(ExceptionHandlers.Info)]
    public ExceptionHandlers ExceptionHandler { get; set; }

    /// <summary>
    /// Delay in seconds between each consume attempt.
    /// </summary>
    /// <example>1, 0.5</example>
    [DefaultValue(1)]
    public double Delay { get; set; }

    /// <summary>
    /// The maximum amount of time in minutes to wait for an event to become available for a given partition before emitting an empty event.
    /// If 0, the processor will wait indefinitely for an event to become available or until Options.MaxEvents is reached.
    /// </summary>
    /// <example>10, 1.5</example>
    public double MaximumWaitTimeInMinutes { get; set; }

    /// <summary>
    /// Count of events to receive before ending this Task.
    /// Unlimited if set to 0.
    /// </summary>
    /// <example>5</example>
    public int MaxEvents { get; set; }
}