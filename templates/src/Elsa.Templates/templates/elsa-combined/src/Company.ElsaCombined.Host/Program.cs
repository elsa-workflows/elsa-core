using Company.ElsaCombined.Host;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.Themes.Extensions;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
#if (withLabels)
using Elsa.Studio.Labels;
#endif
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Translations;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
#else
using Elsa.Extensions;
using Elsa.Http.Options;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Api;
using Microsoft.Extensions.Options;
#endif

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;
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

builder.WebHost.UseStaticWebAssets();
services.AddRazorPages();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin()
    .WithExposedHeaders("*")));
services.AddHealthChecks();

#if (useShellFeatures)
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

services.AddAuthentication();
services.AddAuthorization();
#else
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

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

services.AddControllers();
services.PostConfigure<ApiEndpointOptions>(options => configuration.GetSection("Api").Bind(options));
#endif

#if (useStudioServer)
var useStudioServer = true;
#elif (useStudioWasm)
var useStudioServer = false;
#else
var useStudioServer = configuration.GetValue("Studio:HostingModel", "Wasm").Equals("Server", StringComparison.OrdinalIgnoreCase);
#endif

if (useStudioServer)
{
    services.AddServerSideBlazor(options => options.RootComponents.MaxJSRootComponents = 1000);

    var selectedAuthProvider = ConfigureStudioAuthenticationMode(services, configuration);
    var authenticationHandler = ConfigureStudioAuthentication(services, configuration);
    var backendApiConfig = new BackendApiConfig
    {
        ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
        ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler
    };
    var localizationConfig = new LocalizationConfig
    {
        ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options)
    };

    services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
    services.AddCore().Replace(new(typeof(IBrandingProvider), typeof(StudioBrandingProvider), ServiceLifetime.Scoped));
    services.AddShell(options => configuration.GetSection("Shell").Bind(options));
    services.AddRemoteBackend(backendApiConfig);
    services.AddDashboardModule(backendApiConfig);
    services.AddWorkflowsModule();
#if (withLabels)
    services.AddLabelsModule(backendApiConfig);
#endif
    services.AddLocalizationModule(localizationConfig);
    services.AddTranslations();
    services.AddSignalR(options => options.MaximumReceiveMessageSize = 5 * 1024 * 1000);
    if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
    {
        services
            .AddAuthenticationUI(configuration.GetSection(LoginThemeOptions.SectionName))
            .AddElsaStudioLoginThemes();
    }
}
else
{
    services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseHsts();

app.UseHttpsRedirection();
app.UseCors();
app.MapHealthChecks("/health");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".dat"] = "application/octet-stream"
        }
    }
});
app.UseRouting();

#if (useShellFeatures)
app.MapShells();
app.UseAuthentication();
app.UseAuthorization();
#else
var apiEndpointOptions = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value;
var routePrefix = apiEndpointOptions.RoutePrefix;

app.MapWorkflowsApi(routePrefix);
app.UseAuthentication();
app.UseAuthorization();
app.UseJsonSerializationErrorHandler();
app.UseWorkflows();
app.MapControllers();

if (app.Environment.IsDevelopment())
    app.UseSwaggerUI();
#endif

if (useStudioServer)
{
    app.UseElsaLocalization();
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");
}
else
{
    app.UseBlazorFrameworkFiles();
    app.MapFallbackToPage("/_WasmHost");
}

app.Run();

static Type ConfigureStudioAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var authProvider = configuration["Authentication:Provider"];
    if (string.IsNullOrWhiteSpace(authProvider))
        authProvider = "ElsaIdentity";

    if (authProvider.Equals("ElsaIdentity", StringComparison.OrdinalIgnoreCase))
    {
        services.AddElsaIdentity();
        services.AddElsaIdentityUI();
        return typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler);
    }

    if (authProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
    {
        services.AddOpenIdConnectAuth(options => configuration.GetSection("Authentication:OpenIdConnect").Bind(options));
        return typeof(OidcAuthenticatingApiHttpMessageHandler);
    }

    if (authProvider.Equals("ElsaLogin", StringComparison.OrdinalIgnoreCase))
    {
        services.AddLoginModule().UseElsaIdentity();
        return typeof(AuthenticatingApiHttpMessageHandler);
    }

    throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'.");
}

static StudioAuthenticationProvider ConfigureStudioAuthenticationMode(IServiceCollection services, IConfiguration configuration)
{
    var authProvider = configuration["Authentication:Provider"];
    if (string.IsNullOrWhiteSpace(authProvider))
        authProvider = "ElsaIdentity";

    if (!Enum.TryParse<StudioAuthenticationProvider>(authProvider, true, out var selectedAuthProvider))
        throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'.");

    services.AddStudioAuthenticationMode(options => options.Provider = selectedAuthProvider);
    return selectedAuthProvider;
}
