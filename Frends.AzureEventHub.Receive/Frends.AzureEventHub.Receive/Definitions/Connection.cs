using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Connection parameters for the Event Hub consumer and checkpoint storage.
/// </summary>
public class Connection
{
    // ── Event Hub ────────────────────────────────────────────────────────────

    /// <summary>
    /// Specifies the method used for authenticating to the Event Hub.
    /// Default is AuthenticationMethod.ConnectionString.
    /// </summary>
    /// <example>AuthenticationMethod.ConnectionString</example>
    [DefaultValue(AuthenticationMethod.ConnectionString)]
    public AuthenticationMethod EventHubAuthenticationMethod { get; set; }

    /// <summary>
    /// Specifies the name of the Event Hub to connect to.
    /// </summary>
    /// <example>ExampleHub</example>
    public string EventHubName { get; set; }

    /// <summary>
    /// Specifies the connection string for the Event Hub.
    /// Required when using AuthenticationMethod.ConnectionString.
    /// </summary>
    /// <example>Endpoint=sb://NamespaceName.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=KeyValue</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(EventHubAuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    [PasswordPropertyText]
    public string EventHubConnectionString { get; set; }

    /// <summary>
    /// Specifies the fully qualified namespace of the Event Hub.
    /// Required when using SAS Token or OAuth2 authentication methods.
    /// </summary>
    /// <example>{yournamespace}.servicebus.windows.net</example>
    public string EventHubNamespace { get; set; }

    /// <summary>
    /// Specifies the Shared Access Signature token for the Event Hub.
    /// </summary>
    /// <example>sv=2021-04-10&amp;se=2022-04-10T10%3A431Z&amp;sr=c&amp;sp=l&amp;sig=ZJg983RovE%2BZXI</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(EventHubAuthenticationMethod), "", AuthenticationMethod.SASToken)]
    [PasswordPropertyText]
    public string EventHubSASToken { get; set; }

    /// <summary>
    /// Specifies the Azure Active Directory tenant (directory) ID for Event Hub OAuth2 authentication.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(EventHubAuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string EventHubTenantId { get; set; }

    /// <summary>
    /// Specifies the client (application) ID for Event Hub OAuth2 authentication.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(EventHubAuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string EventHubClientId { get; set; }

    /// <summary>
    /// Specifies the client secret for Event Hub OAuth2 authentication.
    /// </summary>
    /// <example>Password</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(EventHubAuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string EventHubClientSecret { get; set; }

    // ── Checkpoint Storage ───────────────────────────────────────────────────

    /// <summary>
    /// Specifies the method used for authenticating to the checkpoint blob storage.
    /// Default is AuthenticationMethod.ConnectionString.
    /// </summary>
    /// <example>AuthenticationMethod.ConnectionString</example>
    [DefaultValue(AuthenticationMethod.ConnectionString)]
    public AuthenticationMethod StorageAuthenticationMethod { get; set; }

    /// <summary>
    /// The name of the blob container used for checkpointing.
    /// </summary>
    /// <example>examplecontainer</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(StorageAuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    public string ContainerName { get; set; }

    /// <summary>
    /// If true, a new container is created under the specified account if it does not exist.
    /// Not supported when using SAS Token as an authentication method.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool CreateContainer { get; set; }

    /// <summary>
    /// A connection string for the checkpoint blob storage.
    /// </summary>
    /// <example>DefaultEndpointsProtocol=https;AccountName=accountname;AccountKey=Pdlrxyz==;EndpointSuffix=core.windows.net</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(StorageAuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    [PasswordPropertyText]
    public string StorageConnectionString { get; set; }

    /// <summary>
    /// The URI of the blob container.
    /// Required when using SAS Token or OAuth2 authentication method for checkpoint storage.
    /// </summary>
    /// <example>https://{account_name}.blob.core.windows.net/{container_name}</example>
    public string BlobContainerUri { get; set; }

    /// <summary>
    /// Shared access signature token for the checkpoint blob storage.
    /// </summary>
    /// <example>sv=2021-04-10&amp;se=2022-04-10T10%3A431Z&amp;sr=c&amp;sp=l&amp;sig=ZJg9aovE%2BZXI</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(StorageAuthenticationMethod), "", AuthenticationMethod.SASToken)]
    [PasswordPropertyText]
    public string StorageSASToken { get; set; }

    /// <summary>
    /// The Azure Active Directory tenant (directory) ID for checkpoint storage OAuth2 authentication.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(StorageAuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string StorageTenantId { get; set; }

    /// <summary>
    /// The client (application) ID for checkpoint storage OAuth2 authentication.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(StorageAuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string StorageClientId { get; set; }

    /// <summary>
    /// A client secret for checkpoint storage OAuth2 authentication.
    /// </summary>
    /// <example>Password</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(StorageAuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string StorageClientSecret { get; set; }
}
