namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Audit record describing a checkpoint change applied to a single partition.
/// </summary>
public class AppliedTarget
{
    /// <summary>
    /// The partition ID that was updated.
    /// </summary>
    /// <example>0</example>
    public string PartitionId { get; set; }

    /// <summary>
    /// The sequence number the checkpoint held before the update, if it could be determined; otherwise null.
    /// </summary>
    /// <example>1800</example>
    public long? PreviousSequenceNumber { get; set; }

    /// <summary>
    /// The sequence number the checkpoint was set to.
    /// </summary>
    /// <example>1500</example>
    public long NewSequenceNumber { get; set; }
}
