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
    /// List of partition IDs to update. Used with the relative RollbackEvents mode.
    /// Ignored when Targets is provided.
    /// </summary>
    /// <example>[ "0", "1", "2" ]</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string[] PartitionIds { get; set; }

    /// <summary>
    /// Number of events to roll back the checkpoint by (can be 0).
    /// Applies to the relative mode driven by PartitionIds. Ignored when Targets is provided.
    /// </summary>
    /// <example>5</example>
    [DisplayFormat(DataFormatString = "Text")]
    public int RollbackEvents { get; set; }

    /// <summary>
    /// Absolute per-partition rewind targets. When one or more targets are provided,
    /// the Task rewinds each partition to the specified sequence number or enqueued time,
    /// and the relative PartitionIds / RollbackEvents mode is not used.
    /// The partition's current sequence range is resolved by the Task, so no manual
    /// computation against the current position is required.
    /// </summary>
    /// <example>[ { "PartitionId": "0", "TargetSequenceNumber": 1500 } ]</example>
    public PartitionTarget[] Targets { get; set; }
}
