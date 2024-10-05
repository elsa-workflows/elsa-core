using Elsa;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Identity.Multitenancy;
using Elsa.Tenants.Extensions;
using FastEndpoints.Swagger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var multiTenancySection = configuration.GetSection("Multitenancy");

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

    elsa.UseSasTokens()
        .UseIdentity(identity =>
        {
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication();

    elsa.UseTenants(tenantsFeature =>
    {
        tenantsFeature.TenantsOptions = options =>
        {
            multiTenancySection.Bind(options);
            options.TenantResolutionPipelineBuilder.Append<CurrentUserTenantResolver>();
        };
        tenantsFeature.UseConfigurationBasedTenantsProvider();
    });

    elsa
        .UseHttp()
        .AddFastEndpointsAssembly<Program>()
        .UseWorkflowsApi();

    elsa.AddWorkflowsFrom<Program>();
});

builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = documentSetting =>
    {
        documentSetting.Title = "Elsa API";
        documentSetting.Version = "v1";
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWorkflows();
app.UseWorkflowsApi();

if (!app.Environment.IsProduction())
{
    EndpointSecurityOptions.SecurityIsEnabled = false;

    app.UseOpenApi();
    app.UseSwaggerUi();
    app.UseReDoc();
}

app.Run();
