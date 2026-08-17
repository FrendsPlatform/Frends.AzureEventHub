using System;

namespace Frends.AzureEventHub.Receive.Definitions
{
    /// <summary>
    /// Error details.
    /// </summary>
    public class Error
    {
        /// <summary>
        /// The error message.
        /// </summary>
        /// <example>Event processing failed.</example>
        public string Message { get; internal set; }

        /// <summary>
        /// Additional information about the error (the original exception).
        /// </summary>
        /// <example>object { Exception AdditionalInfo }</example>
        public Exception AdditionalInfo { get; internal set; }
    }
}
