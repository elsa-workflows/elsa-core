using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Activities.Http.Services;
using Elsa.Multitenancy;
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
            services.AddElsaServices();

            // Controllers.
            services.AddControllers();

            // Authentication & Authorization.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services
                .AddAuthentication(auth =>
                {
                    auth.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"])),
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                    };
                });

            // Add a custom policy.
            services
                .AddAuthorization(auth => auth
                    .AddPolicy("IsAdmin", policy => policy
                        .RequireClaim("is-admin", "true")));

            services.Configure<JwtOptions>(options => Configuration.GetSection("Jwt").Bind(options));

            // Application Services.
            services.AddSingleton<ITokenService, TokenService>();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            builder.ConfigureElsaServices(services, elsa => elsa
                    .AddHttpActivities(http =>
                    {
                        http.HttpEndpointAuthorizationHandlerFactory =
                            ActivatorUtilities.GetServiceOrCreateInstance<AuthenticationBasedHttpEndpointAuthorizationHandler>;
                        http.BaseUrl = new Uri(Configuration["Elsa:Server:BaseUrl"]);
                        http.BasePath = Configuration["Elsa:Server:BasePath"];
                    }
                    ).AddWorkflowsFrom<Startup>());

            builder.Populate(services);
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

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateSampleMultitenantContainer(container);
        }
    }
}