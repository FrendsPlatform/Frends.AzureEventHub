using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Input parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Specifies the name of the Event Hub to connect to.
    /// </summary>
    /// <example>ExampleHub</example>
    public string EventHubName { get; set; }

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

    /// <summary>
    /// The name of the blob container used for checkpointing.
    /// </summary>
    /// <example>examplecontainer</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ContainerName { get; set; }

    /// <summary>
    /// If true, a new container is created under the specified account if it does not exist.
    /// Not supported when using SAS Token as an authentication method.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool CreateContainer { get; set; }
}
