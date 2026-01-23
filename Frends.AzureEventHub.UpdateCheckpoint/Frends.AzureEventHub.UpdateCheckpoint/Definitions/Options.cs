using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Controls behavior when a specified partition checkpoint does not exist.
    /// If true, the operation fails when a partition is missing.
    /// If false, the partition is skipped and recorded as an error, and processing continues.
    /// When partitions are skipped, the operation is considered unsuccessful; it either
    /// throws or returns a failure result depending on ThrowErrorOnFailure.
    /// This operation is not transactional; updates applied before a failure are not reverted.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool FailIfPartitionMissing { get; set; } = false;

    /// <summary>
    /// True: Throw an exception.
    /// False: Error will be added to the Result.Error.AdditionalInfo list instead of stopping the Task.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Message what will be used when error occurs.
    /// </summary>
    /// <example>Task failed during execution</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ErrorMessageOnFailure { get; set; }
}
