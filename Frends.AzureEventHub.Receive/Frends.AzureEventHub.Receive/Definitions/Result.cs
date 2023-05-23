using System.Collections.Generic;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Receive result.
/// </summary>
public class Result
{
    /// <summary>
    /// Task complete without errors.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; private set; }

    /// <summary>
    /// List of events. 
    /// </summary>
    /// <example>{"foo"}, {"An exception occured"}</example>
    public List<dynamic> Data { get; private set; }

    internal Result(bool success, List<dynamic> data)
    {
        Success = success;
        Data = data;
    }
}