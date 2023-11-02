using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.Receive.Definitions;

/// <summary>
/// Consumer parameters.
/// </summary>
public class Consumer
{
    /// <summary>
    /// Specifies the method used for authentication. 
    /// Default is AuthenticationMethod.ConnectionString.
    /// </summary>
    /// <example>AuthenticationMethod.ConnectionString</example>
    [DefaultValue(AuthenticationMethod.ConnectionString)]
    public AuthenticationMethod AuthenticationMethod { get; set; }

    /// <summary>
    /// Specifies the name of the Event Hub to connect to.
    /// </summary>
    /// <example>ExampleHub</example>
    public string EventHubName { get; set; }

    /// <summary>
    /// Specifies the name of the consumer group for reading events. 
    /// If left empty, the default consumer group will be used.
    /// </summary>
    /// <example>$Default</example>
    public string ConsumerGroup { get; set; }

    /// <summary>
    /// Specifies the connection string for the Event Hub. 
    /// Required when using AuthenticationMethod.ConnectionString.
    /// </summary>
    /// <example>Endpoint=sb://NamespaceName.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=KeyValue</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.ConnectionString)]
    [PasswordPropertyText]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Specifies the fully qualified namespace of the Event Hub. 
    /// Required when using SAS Token or OAuth2 authentication methods.
    /// </summary>
    /// <example>{yournamespace}.servicebus.windows.net</example>
    public string Namespace { get; set; }

    /// <summary>
    /// Specifies the Shared Access Signature token for authentication. 
    /// Access controls may be specified by the Event Hubs namespace or the requested Event Hub, depending on Azure configuration.
    /// </summary>
    /// <example>sv=2021-04-10&amp;se=2022-04-10T10%3A431Z&amp;sr=c&amp;sp=l&amp;sig=ZJg983RovE%2BZXI</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.SASToken)]
    [PasswordPropertyText]
    public string SASToken { get; set; }

    /// <summary>
    /// Specifies the Azure Active Directory tenant (directory) ID for OAuth2 authentication.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string TenantId { get; set; }

    /// <summary>
    /// Specifies the client (application) ID for OAuth2 authentication.
    /// </summary>
    /// <example>Y6b1hf2a-80e2-xyz2-qwer3h-3a7c3a8as4b7f</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientId { get; set; }

    /// <summary>
    /// Specifies the client secret generated for the App Registration used for OAuth2 authentication.
    /// </summary>
    /// <example>Password</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.OAuth2)]
    [PasswordPropertyText]
    public string ClientSecret { get; set; }

    /// <summary>
    /// Specifies the maximum wait time (in seconds) for an event to become available before emitting an empty event.
    /// If set to 0, the processor will wait indefinitely for an event to become available.
    /// Note: `Consumer.MaximumWaitTime` cannot exceed `Options.MaxRunTime` when `Options.MaxRunTime` is greater than 0.
    /// </summary>
    /// <example>0, 10 , 10.1</example>
    public double MaximumWaitTime { get; set; }
}