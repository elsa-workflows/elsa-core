using Elsa;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Tenants.Extensions;
using Elsa.Tenants.Helpers;
using FastEndpoints.Swagger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddElsa(elsa =>
{
    var dbContextOptions = new ElsaDbContextOptions();
    string postgresConnectionString = configuration.GetConnectionString("Postgres")!;
    string schema = configuration.GetConnectionString("Schema")!;

    if (!string.IsNullOrEmpty(schema))
    {
        dbContextOptions.SchemaName = schema;
        dbContextOptions.MigrationsAssemblyName = typeof(Program).Assembly.GetName().Name;
    }
    dbContextOptions.AdditionnalEntityConfigurations = EfCoreDbContextConfiguration.BuildTenantFilterConfiguration();

    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgresConnectionString, dbContextOptions)));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgresConnectionString, dbContextOptions)));

    elsa.UseSasTokens()
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = options => identitySection.Bind(options);
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication();

    elsa.UseTenants(configuration => configuration.UseConfigurationBasedTenantProvider(options => identitySection.Bind(options)));

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
    app.UseSwaggerUi3();
    app.UseReDoc();
}

app.Run();
