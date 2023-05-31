using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Consumer parameters.
/// </summary>
public class Consumer
{
    /// <summary>
    /// Authentication method.
    /// </summary>
    /// <example>AuthenticationMethod.ConnectionString</example>
    [DefaultValue(AuthenticationMethod.ConnectionString)]
    public AuthenticationMethod AuthenticationMethod { get; set; }

    /// <summary>
    /// The name of the specific Event Hub.
    /// </summary>
    /// <example>ExampleHub</example>
    public string EventHubName { get; set; }

    /// <summary>
    /// The name of the consumer group. 
    /// Events are read in the context of this group.
    /// Using default consumer group if left empty.
    /// </summary>
    /// <example>$Default</example>
    public string ConsumerGroup { get; set; }

    /// <summary>
    /// The connection string.
    /// </summary>
    /// <example>Endpoint=sb://NamespaceName.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=KeyValue</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    [PasswordPropertyText]
    public string ConnectionString { get; set; }

    /// <summary>
    /// The fully qualified Event Hubs namespace to connect to.
    /// Required when using SAS Token or OAuth2 authentication method.
    /// </summary>
    /// <example>{yournamespace}.servicebus.windows.net</example>
    public string FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// The shared access signature. 
    /// Access controls may be specified by the Event Hubs namespace or the requested Event Hub, depending on Azure configuration.
    /// </summary>
    /// <example>sv=2021-04-10&amp;se=2022-04-10T10%3A431Z&amp;sr=c&amp;sp=l&amp;sig=ZJg983RovE%2BZXI</example>
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
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientId { get; set; }

    /// <summary>
    /// A client secret that was generated for the App Registration used to authenticate the client.
    /// </summary>
    /// <example>Password</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientSecret { get; set; }

    /// <summary>
    /// The maximum amount of time to wait for an event to become available for a given partition before emitting an empty event.
    /// </summary>
    /// <example>10 , 10.1</example>
    public double MaximumWaitTime { get; set; }
}