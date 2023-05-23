namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Authentication methods.
/// </summary>
public enum AuthenticationMethod
{
#pragma warning disable CS1591 // self explanatory.
    ConnectionString,
    SASToken,
    OAuth2
#pragma warning restore CS1591 // self explanatory
}

/// <summary>
/// How to handle exceptions.
/// </summary>
public enum ExceptionHandlers
{
    /// <summary>
    /// Exception will be added to Result.Data.
    /// </summary>
    Info,

    /// <summary>
    /// Throw an exception.
    /// </summary>
    Throw
}