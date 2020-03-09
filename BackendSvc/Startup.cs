using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

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

            string authProvider = Configuration["AuthProvider"];
            var authConfig = Configuration.GetSection(authProvider);

            // this validates the incoming token is from a trusted issuer
            // and it's issued for this api (audience)
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = authConfig["Authority"];
                    options.RequireHttpsMetadata = false;
                    options.Audience = authConfig["Audience"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        //the issuer in multi-tenant app changes by the tenant
                        ValidateIssuer = false, 
                    };
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
