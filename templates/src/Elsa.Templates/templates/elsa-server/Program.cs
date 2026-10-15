#if (useShellFeatures)
using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Elsa.Dashboard.Api.ShellFeatures;
using Elsa.Identity.ShellFeatures;
using Elsa.ShellFeatures;
using Elsa.Workflows.Api.ShellFeatures;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime.Distributed.ShellFeatures;
using Elsa.Workflows.Runtime.Dashboard.ShellFeatures;
using Elsa.Workflows.Runtime.ShellFeatures;
using Elsa.Workflows.ShellFeatures;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.AddShells(shells => shells
    .WithHostAssemblies()
    .WithConfigurationProvider(configuration)
    .WithWebRouting(options => options.EnablePathRouting = true)
    .WithAuthenticationAndAuthorization()
    .ConfigureAllShells(shell =>
    {
        shell.WithFeatures(
            typeof(ElsaFeature),
            typeof(WorkflowManagementFeature),
            typeof(WorkflowRuntimeFeature),
            typeof(WorkflowsFeature),
            typeof(DistributedRuntimeFeature),
            typeof(WorkflowsApiFeature),
            typeof(DashboardApiFeature),
            typeof(WorkflowRuntimeDashboardFeature),
            typeof(IdentityFeature),
            typeof(DefaultAuthenticationFeature),
            typeof(DefaultAdminUserFeature));
    }));

builder.Services.AddHealthChecks();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.MapHealthChecks("/health");
app.MapShells();
app.UseAuthentication();
app.UseAuthorization();
app.Run();
#else
using Elsa.Extensions;
using Elsa.Http.Options;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Api;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
#if (useSqlitePersistence)
var persistenceConnectionString = configuration.GetConnectionString("Sqlite") ?? throw new InvalidOperationException("Connection string 'Sqlite' is missing.");
#endif
#if (useSqlServerPersistence)
var persistenceConnectionString = configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Connection string 'SqlServer' is missing.");
#endif
#if (usePostgreSqlPersistence)
var persistenceConnectionString = configuration.GetConnectionString("PostgreSql") ?? throw new InvalidOperationException("Connection string 'PostgreSql' is missing.");
#endif
#if (useOraclePersistence)
var persistenceConnectionString = configuration.GetConnectionString("Oracle") ?? throw new InvalidOperationException("Connection string 'Oracle' is missing.");
#endif

services.AddElsa(elsa =>
{
    elsa
        .UseIdentity(identity =>
        {
            identity.TokenOptions += options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflows()
        .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef =>
        {
#if (useSqlitePersistence)
            ef.UseSqlite(persistenceConnectionString);
#endif
#if (useSqlServerPersistence)
            ef.UseSqlServer(persistenceConnectionString);
#endif
#if (usePostgreSqlPersistence)
            ef.UsePostgreSql(persistenceConnectionString);
#endif
#if (useOraclePersistence)
            ef.UseOracle(persistenceConnectionString);
#endif
        }))
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef =>
        {
#if (useSqlitePersistence)
            ef.UseSqlite(persistenceConnectionString);
#endif
#if (useSqlServerPersistence)
            ef.UseSqlServer(persistenceConnectionString);
#endif
#if (usePostgreSqlPersistence)
            ef.UsePostgreSql(persistenceConnectionString);
#endif
#if (useOraclePersistence)
            ef.UseOracle(persistenceConnectionString);
#endif
        }))
        .UseWorkflowsApi()
        .UseDashboardApi()
        .UseWorkflowRuntimeDashboard()
        .UseHttp(http => http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options))
        .UseScheduling()
        .UseJavaScript()
        .UseCSharp()
        .UseLiquid();
});

services.PostConfigure<ApiEndpointOptions>(options => configuration.GetSection("Api").Bind(options));
services.AddControllers();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin()
    .WithExposedHeaders("*")));
services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseCors();
app.MapHealthChecks("/health");

var apiEndpointOptions = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value;
var routePrefix = apiEndpointOptions.RoutePrefix;

app.MapWorkflowsApi(routePrefix);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseJsonSerializationErrorHandler();
app.UseWorkflows();
app.MapControllers();

if (app.Environment.IsDevelopment())
    app.UseSwaggerUI();

await app.RunAsync();
#endif
