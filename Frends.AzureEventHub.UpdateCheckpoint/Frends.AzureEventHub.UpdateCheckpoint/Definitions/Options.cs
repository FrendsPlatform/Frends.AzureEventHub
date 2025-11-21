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
    /// Whether to fail if a specified partition checkpoint does not exist. Default is false.
    /// </summary>
    /// <example>true</example>
    public bool FailIfPartitionMissing { get; set; }

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
