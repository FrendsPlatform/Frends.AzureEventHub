namespace Frends.AzureEventHub.Send.Definitions;

/// <summary>
/// Send result.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the messages were sent successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; init; }

    /// <summary>
    /// Message indicating the result of the operation.
    /// </summary>
    /// <example>A batch of 5 events has been published.</example>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Error information when Success is false and ThrowErrorOnFailure is set to false.
    /// </summary>
    /// <example>null</example>
    public Error Error { get; init; }
}
