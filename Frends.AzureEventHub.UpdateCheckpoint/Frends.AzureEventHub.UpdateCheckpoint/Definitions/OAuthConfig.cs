using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureEventHub.UpdateCheckpoint.Definitions
{
    /// <summary>
    /// OAuth configuration parameters.
    /// </summary>
    public class OAuthConfig
    {
        /// <summary>
        /// The Azure Active Directory tenant ID.
        /// </summary>
        /// <example>a1b2c3d4-5678-90ef-1234-567890abcdef</example>
        [DisplayFormat(DataFormatString = "Text")]
        public string TenantId { get; set; }

        /// <summary>
        /// The application/client ID registered in Azure AD.
        /// </summary>
        /// <example>d4e5f6a7-8901-2345-6789-0b1c2d3e4f5g</example>
        [DisplayFormat(DataFormatString = "Text")]
        public string ClientId { get; set; }

        /// <summary>
        /// The client secret for the registered application.
        /// </summary>
        /// <example>ABC~12345~abcdefghijklmnopqrstuvwxyz67890</example>
        [DisplayFormat(DataFormatString = "Text")]
        [PasswordPropertyText]
        public string ClientSecret { get; set; }
    }
}
