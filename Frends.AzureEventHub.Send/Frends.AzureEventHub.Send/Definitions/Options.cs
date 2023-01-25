using System;
using System.ComponentModel;
using Azure.Messaging.EventHubs;

namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// Input parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// The maximum number of retry attempts. Must be a value between 0 and 100.
    /// </summary>
    /// <example>3</example>
    [DefaultValue(3)]
    public int MaximumRetries { get; set; } = 3;

    /// <summary>
    /// The delay between retry attempts. Must be a value between 1 millisecond and 5 minutes inclusive.
    /// </summary>
    /// <example>800</example>
    [DefaultValue(800)]
    public int MaximumDelayInMilliseconds { get; set; } = 800;
    
    /// <summary>
    /// The maximum delay between retry attempts (e.g. for exponential backoff).
    /// </summary>
    /// <example>5000</example>
    [DefaultValue(60000)]
    public int DelayInMilliseconds { get; set; } = 60000;

    /// <summary>
    /// The maximum duration to wait for completion of a single attempt.
    /// </summary>
    /// <example>5000</example>
    [DefaultValue(60000)]
    public int TryTimeoutInMilliseconds { get; set; } = 60000;

    /// <summary>
    /// The retry mode to use.
    /// </summary>
    /// <example>Exponential</example>
    [DefaultValue(RetryMode.Exponential)]
    public RetryMode RetryMode { get; set; } = RetryMode.Exponential;

    /// <summary>
    /// The transport type to use.
    /// </summary>
    public TransportType TransportType { get; set; } = TransportType.AmqpTcp;

    // Do not make this into a property, Frends UI shows internal properties for some reason
    internal EventHubsRetryMode GetNativeClientRetryMode()
    {
        switch (RetryMode)
        {
            case RetryMode.Exponential:
                return EventHubsRetryMode.Exponential;
            case RetryMode.Fixed:
                return EventHubsRetryMode.Fixed;
            default:
                throw new ArgumentOutOfRangeException(nameof(RetryMode), RetryMode, null);
        }
    }

    // Do not make this into a property, Frends UI shows internal properties for some reason
    internal EventHubsTransportType GetNativeClientTransportType()
    {
        switch (TransportType)
        {
            case TransportType.AmqpTcp:
                return EventHubsTransportType.AmqpTcp;
            case TransportType.AmqpWebSockets:
                return EventHubsTransportType.AmqpWebSockets;
            default:
                throw new ArgumentOutOfRangeException(nameof(TransportType), TransportType, null);
        }
    }
}
