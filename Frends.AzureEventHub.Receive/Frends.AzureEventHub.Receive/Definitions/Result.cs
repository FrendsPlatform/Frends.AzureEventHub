﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Receive result.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the Task completed without errors.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; private set; }

    /// <summary>
    /// Contains a list of events. 
    /// </summary>
    /// <example>{ "foo", bar }</example>
    public List<dynamic> Data { get; private set; }

    /// <summary>
    /// Contains a list of errors If Options.ExceptionHandlers is set to Throw.
    /// </summary>
    /// <example>{ "An exception occured", "Another exception occured" }</example>
    public List<dynamic> Errors { get; private set; }

    internal Result(bool success, ConcurrentBag<dynamic> data, ConcurrentBag<dynamic> errors)
    {
        Success = success;
        Data = data.ToList();
        Errors = errors.ToList();
    }
}