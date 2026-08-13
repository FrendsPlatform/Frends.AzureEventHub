using System.ComponentModel;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Input parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Specifies the name of the consumer group for reading events.
    /// If left empty, the default consumer group will be used.
    /// </summary>
    /// <example>$Default</example>
    public string ConsumerGroup { get; set; }

    /// <summary>
    /// Specifies the maximum wait time (in seconds) for an event to become available before emitting an empty event.
    /// If set to 0, the processor will wait indefinitely for an event to become available.
    /// Note: MaximumWaitTime cannot exceed Options.MaxRunTime when Options.MaxRunTime is greater than 0.
    /// </summary>
    /// <example>0, 10, 10.1</example>
    [DefaultValue((double)0)]
    public double MaximumWaitTime { get; set; }
}
