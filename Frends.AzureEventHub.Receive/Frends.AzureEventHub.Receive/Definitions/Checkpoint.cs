using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Checkpoint parameters.
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// Authentication method.
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
    /// If true, operation creates a new container under the specified account. 
    /// If the container with the same name already exists, it is not changed.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool CreateContainer { get; set; }

    /// <summary>
    /// A connection string.
    /// </summary>
    /// <example>DefaultEndpointsProtocol=https;AccountName=accountname;AccountKey=Pdlrxyz==;EndpointSuffix=core.windows.net</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    [PasswordPropertyText]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Referencing the blob container that includes the name of the account and the name of the container.
    /// Required when using SAS Token or OAuth2 authentication method.
    /// </summary>
    /// <example>https://{account_name}.blob.core.windows.net/{container_name}</example>
    public string BlobContainerUri { get; set; }

    /// <summary>
    /// Shared access signature.
    /// </summary>
    /// <example>sv=2021-04-10&amp;se=2022-04-10T10%3A431Z&amp;sr=c&amp;sp=l&amp;sig=ZJg9aovE%2BZXI</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.SASToken)]
    [PasswordPropertyText]
    public string SASToken { get; set; }

    /// <summary>
    /// The Azure Active Directory tenant (directory) Id.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string TenantId { get; set; }

    /// <summary>
    /// The client (application) ID.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientId { get; set; }

    /// <summary>
    /// A client secret.
    /// </summary>
    /// <example>Password</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientSecret { get; set; }
}