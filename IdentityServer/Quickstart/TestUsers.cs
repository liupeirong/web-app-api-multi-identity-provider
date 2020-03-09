// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer
{
    public class TestUsers
    {
        public static List<TestUser> Users = new List<TestUser>
        {
            new TestUser{SubjectId = "818727", Username = "zoe", Password = "zoe", 
                Claims = 
                {
                    new Claim(JwtClaimTypes.Name, "Zoe Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Zoe"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "ZoeSmith@email.com"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.WebSite, "http://zoe.com"),
                    new Claim(JwtClaimTypes.Address, @"{ 'street_address': '100 Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json),
                    new Claim("contract", "pro")
                }
            },
            new TestUser{SubjectId = "88421113", Username = "will", Password = "will", 
                Claims = 
                {
                    new Claim(JwtClaimTypes.Name, "Will Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Will"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "WillSmith@email.com"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.WebSite, "http://will.com"),
                    new Claim(JwtClaimTypes.Address, @"{ 'street_address': '200 Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json),
                    new Claim("location", "somewhere"),
                    new Claim("contract", "trial")
                }
            }
        };
    }
}