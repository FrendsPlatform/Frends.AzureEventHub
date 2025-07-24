using Microsoft.Azure.Amqp.Framing;
using System;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether update checkpoint operration was  successfull.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// List of partitions that were successfully updated.
    /// </summary>
    /// <example>[ "0", "1" ]</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string[] UpdatedPartitions { get; set; }

    /// <summary>
    /// List of partitions skipped due to missing checkpoints or errors.
    /// </summary>
    /// <example>[ "2" ]</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string[] SkippedPartitions { get; set; }

    /// <summary>
    /// Indicates if rollback was applied.
    /// </summary>
    /// <example>true</example>
    public bool RollbackApplied { get; set; }

    /// <summary>
    /// List of error messages, if any, per partition.
    /// </summary>
    /// <example>[ { "PartitionId": "2", "Error": "Checkpoint not found." } ]</example>
    public Error[] Errors { get; set; }

}
