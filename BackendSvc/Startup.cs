using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace BackendSvc
{
    public class Startup
    {
        IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            var authnBuilder = services.AddAuthentication();

            var authProviders = Configuration.GetSection("TrustedAuthProviders").GetChildren();
            List<string> schemes = new List<string>();
            foreach(var provider in authProviders)
            {
                string scheme = provider.GetValue<string>("scheme");
                // validate the token comes from trusted authority and is issued
                // to expected audience
                authnBuilder.AddJwtBearer(scheme, options =>
                {
                    options.Authority = provider.GetValue<string>("authority");
                    options.RequireHttpsMetadata = false;
                    options.Audience = provider.GetValue<string>("audience");
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        //the issuer in multi-tenant app changes by the tenant
                        ValidateIssuer = false, 
                    };
                });
                schemes.Add(scheme);
            }

            services.AddAuthorization(options =>
                {
                    var  authPolicyBuilder = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(schemes.ToArray());
                    options.DefaultPolicy = authPolicyBuilder.Build();
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello, call api/blobstore/blobs");
                });
                endpoints.MapControllers()
                    .RequireAuthorization();
            });
        }
    }
}
