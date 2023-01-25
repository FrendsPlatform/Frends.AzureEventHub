namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// The retry mode.
/// </summary>
public enum RetryMode {
    /// <summary>
    /// The default retry mode, which is fixed.
    /// </summary>
    Fixed,

    /// <summary>
    /// The retry mode that uses an exponential backoff.
    /// </summary>
    Exponential
}
