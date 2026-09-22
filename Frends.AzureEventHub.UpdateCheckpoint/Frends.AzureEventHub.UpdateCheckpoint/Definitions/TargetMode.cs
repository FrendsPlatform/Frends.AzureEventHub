namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Selects how a single partition's checkpoint is rewound.
/// </summary>
public enum TargetMode
{
    /// <summary>
    /// Roll back the checkpoint by a number of events relative to its current position.
    /// </summary>
    RelativeRollback,

    /// <summary>
    /// Set the checkpoint to an exact sequence number.
    /// </summary>
    AbsoluteSequenceNumber,

    /// <summary>
    /// Set the checkpoint to the first event enqueued at or after a specific point in time.
    /// </summary>
    AbsoluteEnqueuedTime,
}
