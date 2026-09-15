using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Rewind target for a single partition. Select Mode to determine how the
/// partition's checkpoint is rewound; only the property matching the selected mode is used.
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
    /// Selects how the checkpoint is rewound for this partition:
    /// RelativeRollback rolls back by a number of events,
    /// AbsoluteSequenceNumber sets an exact sequence number,
    /// AbsoluteEnqueuedTime sets the checkpoint to the first event enqueued at or after a point in time.
    /// </summary>
    /// <example>TargetMode.RelativeRollback</example>
    [DefaultValue(TargetMode.RelativeRollback)]
    public TargetMode Mode { get; set; } = TargetMode.RelativeRollback;

    /// <summary>
    /// Number of events to roll back the checkpoint by (can be 0).
    /// Used when Mode is RelativeRollback; ignored otherwise.
    /// </summary>
    /// <example>5</example>
    [UIHint(nameof(Mode), "", TargetMode.RelativeRollback)]
    public int RollbackEvents { get; set; }

    /// <summary>
    /// The absolute sequence number to set the checkpoint to.
    /// Used when Mode is AbsoluteSequenceNumber; ignored otherwise.
    /// </summary>
    /// <example>1500</example>
    [UIHint(nameof(Mode), "", TargetMode.AbsoluteSequenceNumber)]
    public long? TargetSequenceNumber { get; set; }

    /// <summary>
    /// The point in time to rewind to. The Task reads the first event enqueued at or after
    /// this time and checkpoints that event's position.
    /// Used when Mode is AbsoluteEnqueuedTime; ignored otherwise.
    /// </summary>
    /// <example>2026-01-01T00:00:00Z</example>
    [UIHint(nameof(Mode), "", TargetMode.AbsoluteEnqueuedTime)]
    public DateTimeOffset? TargetEnqueuedTime { get; set; }
}
