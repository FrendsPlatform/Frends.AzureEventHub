namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Authentication methods.
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// Connection string.
    /// See https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-get-connection-string
    /// </summary>
    ConnectionString,

    /// <summary>
    /// SAS Token.
    /// See https://learn.microsoft.com/en-us/azure/cognitive-services/translator/document-translation/how-to-guides/create-sas-tokens?tabs=Containers
    /// </summary>
    SASToken,

    /// <summary>
    /// OAuth2.
    /// See https://learn.microsoft.com/en-us/azure/active-directory/fundamentals/auth-oauth2
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