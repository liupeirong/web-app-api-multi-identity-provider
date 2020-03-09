// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace IdentityServer
{
    public static class Config
    {
        public static IConfiguration Configuration;

        public static IEnumerable<IdentityResource> Ids =>
            new IdentityResource[]
            { 
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("business", "business contract", new string[]{"contract"}),
                new IdentityResource("aadtenant", "aad non-standard claims", new string[]{"oid", "tid"}),
            };

        public static IEnumerable<ApiResource> Apis =>
            new ApiResource[] { };
        
        public static IEnumerable<Client> Clients =>
            new Client[] 
            { 
                // interactive ASP.NET Core MVC client
                new Client
                {
                    ClientId = "mvc",
                    ClientSecrets = { new Secret("secret".Sha256()) },

                    //Implicit only generates id_token
                    //Code generates both id_token and access_token
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials, //GrantTypes.Implicit},
                    RequireConsent = false,
                    RequirePkce = true,
                    AlwaysIncludeUserClaimsInIdToken = true,

                    // where to redirect to after login
                    RedirectUris = { Configuration["MvcClient:RedirectUri"]},

                    // where to redirect to after logout
                    PostLogoutRedirectUris = { Configuration["MvcClient:PostLogoutRedirectUri"] },

                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "business",
                        "aadtenant",
                    }

                }
            };
        
    }
}