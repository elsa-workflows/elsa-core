using System.Text;
using Elsa.Samples.HttpEndpointSecurity.Options;
using Elsa.Samples.HttpEndpointSecurity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Samples.HttpEndpointSecurity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Controllers.
            services.AddControllers();
            
            // Authentication & Authorization.
            services
                .AddAuthentication(auth => { auth.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"]))
                    };
                });

            services.AddAuthorization();

            services.Configure<JwtOptions>(options => Configuration.GetSection("Jwt").Bind(options));

            // Elsa.
            services
                .AddElsa(elsa => elsa
                    .AddHttpActivities(http => Configuration.GetSection("Elsa:Server").Bind(http))
                    .AddWorkflowsFrom<Startup>()
                );

            // Application Services.
            services.AddSingleton<ITokenService, TokenService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseHttpActivities()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}