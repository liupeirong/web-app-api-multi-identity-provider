using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using IdentityServer4.Test;
using IdentityServer4.Extensions;
using System.Linq;

public class MyCustomProfileService : IProfileService
{
    protected readonly ILogger Logger;
    protected readonly TestUserStore Users;

    public MyCustomProfileService(TestUserStore users, ILogger<MyCustomProfileService> logger){
        Users = users;
        Logger = logger;
    }

    public Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        context.LogProfileRequest(Logger);

        if (context.RequestedClaimTypes.Any())
        {
            var user = Users.FindBySubjectId(context.Subject.GetSubjectId());
            if (user != null)
            {
                // AddRequestedClaims will filter out any claims that's not in standard ScopeToClaimsMapping
                // defined in IdentityServer4 Constants.cs
                // Since we added "aadtenant" scope to include aad specific claims oid and tid, 
                // they won't be filtered out.
                context.AddRequestedClaims(user.Claims);
            }
        }

        context.LogIssuedClaims(Logger);

        return Task.CompletedTask;
    }

    public Task IsActiveAsync(IsActiveContext context)
    {
        context.IsActive = true;
        return Task.CompletedTask;
    }
}