using System.Runtime.Serialization;

namespace IdentityServerAPI.Models
{
    [DataContract]
    public class OAuth2TokenResponse
    {
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "expires_in")]
        public int ExpirationInSeconds { get; set; }
    }
}