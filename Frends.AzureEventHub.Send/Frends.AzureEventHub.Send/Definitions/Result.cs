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
    public bool Success { get; private set; }

    /// <summary>
    /// Message indicating the result of the operation.
    /// </summary>
    /// <example>A batch of 5 events has been published.</example>
    public string Message { get; private set; }

    internal Result(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}
