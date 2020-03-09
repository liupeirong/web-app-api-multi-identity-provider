using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace MvcClient
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            String authProvider = Configuration["AuthProvider"];
            var idpConfig = Configuration.GetSection(authProvider);

            var builder = services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = authProvider;
                })
                .AddCookie("Cookies");

            switch (authProvider)
            {
                case "myidsrv":
                    builder.AddOpenIdConnect(authProvider, "my custom identityserver", options =>
                    {
                        options.Authority = idpConfig["url"]; 
                        options.RequireHttpsMetadata = false;
                        options.ClientId = idpConfig["clientId"];
                        options.ClientSecret = idpConfig["clientSecret"];

                        // ResponseType must match AllowedGrantTypes in the client config on the server side
                        // "id_token" can be generated with implicit flow in idsrv, 
                        // "code" uses auth code flow, which will generate the id_token and access_token
                        // "code" requires clientSecret to work
                        options.ResponseType = "code";
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                        options.Scope.Add("business");
                        options.Scope.Add("aadtenant");

                        options.SaveTokens = true;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
                        options.ClaimActions.MapUniqueJsonKey("oid", "oid");
                        options.ClaimActions.MapUniqueJsonKey("tid", "tid");
                        options.ClaimActions.MapUniqueJsonKey("contract", "contract");
                    });
                    break;
                case "aad":
                    builder.AddOpenIdConnect(authProvider, "Azure AD", options =>
                    {
                        options.Authority = idpConfig["url"];
                        options.ClientId = idpConfig["clientId"];
                        //options.ClientSecret = idpConfig["clientSecret"];

                        options.ResponseType = "id_token";
                        options.CallbackPath = "/signin-aad";
                        options.SignedOutCallbackPath = "/signout-callback-aad";
                        options.RemoteSignOutPath = "/signout-aad";

                        options.SaveTokens = true; // this will cause tokens to show up in properties

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidAudience = Configuration["aadClientId"],

                            NameClaimType = "name",
                            RoleClaimType = "role"
                        };
                    });
                    break;
                case "google":
                    builder.AddGoogle(authProvider, options =>
                    {
                        options.ClientId = idpConfig["clientId"];
                        options.ClientSecret = idpConfig["clientSecret"];

                        options.SaveTokens = true;
                    });
                    break;
                default:
                    throw new Exception("No authentication scheme configured, exiting.");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=PublicIndex}/{id?}");
                    //.RequireAuthorization();
            });
        }
    }
}
