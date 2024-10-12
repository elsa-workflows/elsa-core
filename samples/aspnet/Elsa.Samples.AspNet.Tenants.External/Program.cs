using System.Security.Claims;
using Elsa;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Identity.Multitenancy;
using Elsa.Tenants.Extensions;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

var identitySection = configuration.GetSection("Identity");
var tenantsSection = configuration.GetSection("Multitenancy");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddElsa(elsa =>
{
    var dbContextOptions = new ElsaDbContextOptions();
    string sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
    string schema = configuration.GetConnectionString("Schema")!;

    if (!string.IsNullOrEmpty(schema))
    {
        dbContextOptions.SchemaName = schema;
        dbContextOptions.MigrationsAssemblyName = typeof(Program).Assembly.GetName().Name;
    }

    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString, dbContextOptions)));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString, dbContextOptions)));
    elsa.UseIdentity(options => identitySection.Bind(options));
    elsa.UseTenants(tenantsFeature =>
    {
        tenantsFeature.TenantsOptions = options =>
        {
            tenantsSection.Bind(options);
            options.TenantResolverPipelineBuilder.Append<ClaimsTenantResolver>();
        };
        tenantsFeature.UseConfigurationBasedTenantsProvider();
    });

    elsa
        .UseHttp(options =>
        {
            options.ConfigureHttpOptions = httpOptions =>
            {
                httpOptions.BaseUrl = new Uri("https://localhost:8765");
                httpOptions.BasePath = "/workflows-http-endpoints";
            };
        })
        .AddFastEndpointsAssembly<Program>()
        .UseWorkflowsApi()
        .UseScheduling()
        .UseRealTimeWorkflows()
        .UseJavaScript()
        .UseLiquid();
});

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = configuration.GetValue<string>("Authentication:Authority");
        options.Audience = configuration.GetValue<string>("Authentication:Audience");
        options.RequireHttpsMetadata = false;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = (context) =>
            {
                //Simplification of the Elsa permissions by granting access to everything
                var identity = context.Principal.Identity as ClaimsIdentity;
                identity?.AddClaim(new Claim("permissions", PermissionNames.All));
                return Task.CompletedTask;
            }
        };
    });

builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = documentSetting =>
    {
        documentSetting.Title = "Elsa API";
        documentSetting.Version = "v1";

        documentSetting.AddSecurity("bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.OAuth2,
            Description = "OIDC",
            Flow = OpenApiOAuth2Flow.Implicit,
            Flows = new OpenApiOAuthFlows()
            {
                AuthorizationCode = new OpenApiOAuthFlow()
                {
                    AuthorizationUrl = $"{configuration.GetValue<string>("Authentication:Authority")}/connect/authorize",
                    TokenUrl = $"{configuration.GetValue<string>("Authentication:Authority")}/connect/token",
                    Scopes = new Dictionary<string, string>
                    {
                        {
                            "workflows.*", "workflows.*"
                        },
                        {
                            "openid", "OpenId"
                        },
                        {
                            "profile", "Profile"
                        },
                    },
                },
            }
        });
        documentSetting.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));
    };
});

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseTenants();
app.MapControllers();
app.UseCors();
app.UseWorkflows();
app.UseWorkflowsApi("api");
app.UseWorkflowsSignalRHubs();

if (!app.Environment.IsProduction())
{
    app.UseOpenApi(options => options.PostProcess = (document, _) => document.Servers.Clear());
    app.UseSwaggerUi(options =>
    {
        options.OAuth2Client = new OAuth2ClientSettings
        {
            ClientId = configuration.GetValue<string>("Authentication:ClientId"),
            ClientSecret = configuration.GetValue<string>("Authentication:ClientSecret"),
            AppName = configuration.GetValue<string>("Authentication:Audience"),
            UsePkceWithAuthorizationCodeGrant = true,
        };
        options.OAuth2Client.Scopes.AddRange("workflows.*", "openid", "profile");
    });
    app.UseReDoc();
}

app.Run();