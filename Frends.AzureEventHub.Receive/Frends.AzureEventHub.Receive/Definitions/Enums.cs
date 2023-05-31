namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Authentication methods.
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// Connection string.
    /// </summary>
    ConnectionString,

    /// <summary>
    /// SAS Token.
    /// </summary>
    SASToken,

    /// <summary>
    /// OAuth2.
    /// </summary>
    OAuth2
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