namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// Transport type for the connection.
/// </summary>
public enum TransportType
{
    /// <summary>
    /// AMQP over TCP
    /// </summary>
    AmqpTcp,

    /// <summary>
    /// AMQP over WebSockets
    /// </summary>
    AmqpWebSockets
}
