using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// The name of the Azure Event Hub.
    /// </summary>
    /// <example>myeventhub</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string EventHubName { get; set; }

    /// <summary>
    /// The consumer group whose checkpoint should be updated.
    /// </summary>
    /// <example>$Default</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ConsumerGroup { get; set; }

    /// <summary>
    /// Per-partition checkpoint targets. Each target selects a partition and a positioning mode
    /// (relative rollback, absolute sequence number, or absolute enqueued time). At least one
    /// target must be provided. The target position is validated automatically against the partition's available
    /// sequence range, so no manual lookup of the current checkpoint or sequence numbers is required.
    /// </summary>
    /// <example>[ { "PartitionId": "0", "Mode": "AbsoluteSequenceNumber", "TargetSequenceNumber": 1500 } ]</example>
    public PartitionTarget[] Targets { get; set; }
}
