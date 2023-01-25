namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// Represents a message for Event Hub.
/// </summary>
public class EventHubMessage
{
    /// <summary>
    /// Message to be sent to Event Hub.
    /// </summary>
    /// <example>Hello, event hub!</example>
    public string Message { get; set; }
}
