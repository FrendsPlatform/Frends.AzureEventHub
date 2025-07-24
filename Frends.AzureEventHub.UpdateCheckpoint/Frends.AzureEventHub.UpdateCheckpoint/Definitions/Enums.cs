using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions
{
    /// <summary>
    /// Contains enums used for configuration and authentication methods.
    /// </summary>
    public class Enums
    {
        /// <summary>
        /// Specifies the authentication method for connecting to Azure services.
        /// </summary>
        public enum AuthMethod
        {
            /// <summary>
            /// Authenticate using a connection string (contains key/secret).
            /// </summary>
            /// <example>Endpoint=sb://myservicebus.servicebus.windows.net/;SharedAccessKeyName=MyPolicy;SharedAccessKey=abc123...</example>
            ConnectionString,

            /// <summary>
            /// Authenticate using a short-lived SAS (Shared Access Signature) token.
            /// </summary>
            /// <example>?sv=2023-01-01sig=ABC123def456ghi789jkl012se=2024-01-01T00:00:00Z</example>
            SasToken,

            /// <summary>
            /// Authenticate using OAuth 2.0 (Azure AD credentials).
            /// </summary>
            /// <example>Requires TenantId, ClientId, and ClientSecret</example>
            OAuth,
        }
    }
}
