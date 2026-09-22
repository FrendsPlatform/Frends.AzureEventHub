using System;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Thrown when a partition targeted with RelativeRollback mode has no existing checkpoint to roll back from.
/// Distinguished from other failures so that Options.FailIfPartitionMissing can stop further processing.
/// </summary>
public class PartitionMissingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionMissingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PartitionMissingException(string message)
        : base(message)
    {
    }
}
