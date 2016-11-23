
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADFSmockup_IdentityServer
{
    public class Config
    {
        public static IEnumerable<Scope> GetScopes()
        {
            return new List<Scope>
            {
                new Scope
                {
                    Name = "CMSScope",
                    Description = "Access to my CMS.",
                    IncludeAllClaimsForUser = true
                }
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = "StaffName",

                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("pwd1".Sha256())
                    },

                    Claims = new []
                    {
                        new System.Security.Claims.Claim("Group", "Staff")
                    },

                    // scopes that client has access to
                    AllowedScopes = { "CMSScope" }
                },
                new Client
                {
                    ClientId = "ManagementName",
                    
                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    
                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("pwd2".Sha256())
                    },

                    Claims = new []
                    {
                        //You can use claims to give clients access to more groups.
                        //new System.Security.Claims.Claim("Group", "Staff"),
                        new System.Security.Claims.Claim("Group", "Management")

                    },

                    // scopes that client has access to
                    AllowedScopes = { "CMSScope" }
                }
            };
        }
    }
}
