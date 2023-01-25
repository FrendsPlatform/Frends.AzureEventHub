using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// Input parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Messages to be sent to Event Hub
    /// </summary>
    /// <example>[ { Message = "Hello" }, { Message = "World } ]</example>
    public EventHubMessage[] Messages { get; set; }

    /// <summary>
    /// Event Hub connection string.
    /// </summary>
    /// <example>my-event-hub</example>
    [DisplayFormat(DataFormatString = "Text")]
    [PasswordPropertyText]
    public string ConnectionString { get; set; }
}
