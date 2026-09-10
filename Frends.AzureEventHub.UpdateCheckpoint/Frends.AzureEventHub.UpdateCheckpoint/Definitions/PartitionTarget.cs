using System;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Absolute rewind target for a single partition.
/// Provide either <see cref="TargetSequenceNumber"/> or <see cref="TargetEnqueuedTime"/>.
/// </summary>
public class PartitionTarget
{
    /// <summary>
    /// The partition ID to rewind.
    /// </summary>
    /// <example>0</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string PartitionId { get; set; }

    /// <summary>
    /// The absolute sequence number to set the checkpoint to.
    /// Leave null to target by <see cref="TargetEnqueuedTime"/> instead.
    /// </summary>
    /// <example>1500</example>
    public long? TargetSequenceNumber { get; set; }

    /// <summary>
    /// The point in time to rewind to. The Task reads the first event enqueued at or after
    /// this time and checkpoints that event's position.
    /// Leave null to target by <see cref="TargetSequenceNumber"/> instead.
    /// </summary>
    /// <example>2026-01-01T00:00:00Z</example>
    public DateTimeOffset? TargetEnqueuedTime { get; set; }
}
