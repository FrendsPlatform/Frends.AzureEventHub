using System.ComponentModel;
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
    /// List of partition IDs to update.
    /// </summary>
    /// <example>[ "0", "1", "2" ]</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string[] PartitionIds { get; set; }

    /// <summary>
    /// Number of events to roll back the checkpoint by (can be 0).
    /// </summary>
    /// <example>5</example>
    [DisplayFormat(DataFormatString = "Text")]
    public int RollbackEvents { get; set; }
}
