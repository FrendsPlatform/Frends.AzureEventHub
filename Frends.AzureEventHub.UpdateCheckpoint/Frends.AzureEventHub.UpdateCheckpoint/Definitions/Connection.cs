using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using static Frends.AzureEventHub.UpdateCheckpoint.Definitions.Enums;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions;

/// <summary>
/// Connection parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// Name of the Azure Storage Account.
    /// </summary>
    /// <example>mystorageaccount</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string StorageAccountName { get; set; }

    /// <summary>
    /// Name of the blob container used for checkpoints.
    /// </summary>
    /// <example>checkpoint-container</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ContainerName { get; set; }

    /// <summary>
    /// Namespace of the Event Hub.
    /// </summary>
    /// <example>myeventhubnamespace</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string EventHubNamespace { get; set; }

    /// <summary>
    /// Authentication method to use.
    /// </summary>
    /// <example>ConnectionString</example>
    public AuthMethod AuthMethod { get; set; }

    /// <summary>
    /// Full connection string to the Azure Storage account.
    /// Used if AuthMethod is ConnectionString.
    /// </summary>
    /// <example>DefaultEndpointsProtocol=https;AccountName=mystorage;AccountKey=KEY;EndpointSuffix=core.windows.net</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthMethod), "", AuthMethod.ConnectionString)]
    [PasswordPropertyText]
    public string ConnectionString { get; set; }

    /// <summary>
    /// SAS token for accessing the blob storage.
    /// Used if AuthMethod is SasToken.
    /// </summary>
    /// <example>?sv=2020-08-04ss=bsrt=scosp=rw</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthMethod), "", AuthMethod.SasToken)]
    [PasswordPropertyText]
    public string SasToken { get; set; }

    /// <summary>
    /// OAuth configuration: includes tenant ID, client ID, client secret, etc.
    /// Used if AuthMethod is OAuth.
    /// </summary>
    /// <example>{ "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", "ClientId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy" }</example>
    [UIHint(nameof(AuthMethod), "", AuthMethod.OAuth)]
    public OAuthConfig OAuth { get; set; }
}
