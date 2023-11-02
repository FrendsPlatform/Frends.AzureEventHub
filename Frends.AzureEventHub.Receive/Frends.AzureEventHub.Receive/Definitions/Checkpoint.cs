using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Checkpoint parameters.
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// Specifies the method used for authentication. 
    /// Default is AuthenticationMethod.ConnectionString.
    /// </summary>
    /// <example>AuthenticationMethod.ConnectionString</example>
    [DefaultValue(AuthenticationMethod.ConnectionString)]
    public AuthenticationMethod AuthenticationMethod { get; set; }

    /// <summary>
    /// The name of the blob container.
    /// </summary>
    /// <example>examplecontainer</example>
    public string ContainerName { get; set; }

    /// <summary>
    /// If true, a new container is created under the specified account if it doesn’t exist. 
    /// Not supported when using SAS Token as an authentication method.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool CreateContainer { get; set; }

    /// <summary>
    /// A connection string for the blob container.
    /// </summary>
    /// <example>DefaultEndpointsProtocol=https;AccountName=accountname;AccountKey=Pdlrxyz==;EndpointSuffix=core.windows.net</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    [PasswordPropertyText]
    public string ConnectionString { get; set; }

    /// <summary>
    /// The URI of the blob container. 
    /// Required when using SAS Token or OAuth2 authentication method.
    /// </summary>
    /// <example>https://{account_name}.blob.core.windows.net/{container_name}</example>
    public string BlobContainerUri { get; set; }

    /// <summary>
    /// Shared access signature token.
    /// </summary>
    /// <example>sv=2021-04-10&amp;se=2022-04-10T10%3A431Z&amp;sr=c&amp;sp=l&amp;sig=ZJg9aovE%2BZXI</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.SASToken)]
    [PasswordPropertyText]
    public string SASToken { get; set; }

    /// <summary>
    /// The Azure Active Directory tenant (directory) Id. 
    /// Required for OAuth2 authentication method.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string TenantId { get; set; }

    /// <summary>
    /// The client (application) ID. 
    /// Required for OAuth2 authentication method.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientId { get; set; }

    /// <summary>
    /// A client secret.
    /// Required for OAuth2 authentication method.
    /// </summary>
    /// <example>Password</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientSecret { get; set; }
}