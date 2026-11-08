using System.Text.Encodings.Web;
using Elsa.Extensions;
using Elsa.Http.Options;
using Elsa.Identity.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.Themes.Extensions;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Settings.Extensions;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Translations;
using Elsa.Studio.Workflows.ActivityPickers.Treeview;
using Elsa.Studio.Workflows.Dashboard.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Api;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Options;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")
    ?? "Data Source=App_Data/elsa.sqlite.db;Cache=Shared;";

builder.WebHost.UseStaticWebAssets();
services.AddResponseCompression();
services.AddRazorPages();
services.AddServerSideBlazor(options =>
{
    options.RootComponents.RegisterCustomElsaStudioElements();
    options.RootComponents.MaxJSRootComponents = 1000;
});
services.AddControllers();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin()
    .WithExposedHeaders("*")));
services
    .AddHealthChecks()
    .AddElsaReadinessChecks(includeDistributedLocks: false);
services.PostConfigure<ApiEndpointOptions>(options => configuration.GetSection("Api").Bind(options));

services.AddElsa(elsa =>
{
    elsa
        .UseIdentity(identity =>
        {
            identity.TokenOptions += options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));

            var adminUserName = configuration["DefaultAdminUser:AdminUserName"];
            var adminPassword = configuration["DefaultAdminUser:AdminPassword"];
            if (!string.IsNullOrWhiteSpace(adminUserName) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                identity.UseDefaultAdmin(admin => admin
                    .WithAdminUserName(adminUserName)
                    .WithAdminPassword(adminPassword)
                    .WithAdminRoleName(configuration["DefaultAdminUser:AdminRoleName"] ?? "admin")
                    .WithAdminRolePermissions(["*"]));
            }
        })
        .UseDefaultAuthentication()
        .UseWorkflows()
        .UseFlowchart(flowchart => flowchart.UseTokenBasedExecution())
        .UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            management.UseCache();
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            runtime.UseCache();
        })
        .UseWorkflowsApi()
        .UseDashboardApi()
        .UseWorkflowRuntimeDashboard()
        .UseScheduling()
        .UseCSharp(csharp => csharp.CSharpOptions += options => configuration.GetSection("Scripting:CSharp").Bind(options))
        .UseJavaScript()
        .UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default)
        .UsePython(python => python.PythonOptions += options => configuration.GetSection("Scripting:Python").Bind(options))
        .UseHttp(http =>
        {
            http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);
            http.UseCache();
        });
});

services.Configure<IncidentOptions>(options => options.DefaultIncidentStrategy = typeof(ContinueWithIncidentsStrategy));

var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
    authProvider = "ElsaIdentity";
if (!Enum.TryParse<StudioAuthenticationProvider>(authProvider, true, out var selectedAuthProvider)
    || (selectedAuthProvider is not StudioAuthenticationProvider.ElsaIdentity and not StudioAuthenticationProvider.ElsaLogin))
{
    throw new InvalidOperationException(
        $"Unsupported Authentication:Provider value '{authProvider}'. The combined host supports 'ElsaIdentity' and 'ElsaLogin'.");
}

services.AddStudioAuthenticationMode(options => options.Provider = selectedAuthProvider);

Type authenticationHandler;
if (selectedAuthProvider == StudioAuthenticationProvider.ElsaIdentity)
{
    services.AddElsaIdentity();
    services.AddElsaIdentityUI();
    authenticationHandler = typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler);
}
else
{
    services.AddLoginModule().UseElsaIdentity();
    authenticationHandler = typeof(AuthenticatingApiHttpMessageHandler);
}

var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler
};
var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options =>
    {
        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
        options.SupportedCultures =
        [
            options.DefaultCulture,
            .. (options.SupportedCultures ?? []).Where(culture => culture != options.DefaultCulture)
        ];
    }
};

services.AddCore(options => configuration.GetSection(StudioThemeOptions.SectionName).Bind(options));
services.AddShell(options => configuration.GetSection("Shell").Bind(options));
if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
{
    services
        .AddAuthenticationUI(configuration.GetSection(LoginThemeOptions.SectionName))
        .AddElsaStudioLoginThemes();
}

services.AddRemoteBackend(backendApiConfig);
services.AddSettingsModule();
services.AddSecurityModule(backendApiConfig);
services.AddDashboardModule(backendApiConfig);
services.AddWorkflowsModule();
services.AddWorkflowsDashboardModule();
services.AddLocalizationModule(localizationConfig);
services.AddTranslations();
services.AddScoped<IActivityPickerComponentProvider, TreeviewActivityPickerComponentProvider>();
services.Configure<DesignerOptions>(configuration.GetSection("DesignerOptions"));
services.AddSignalR(options => options.MaximumReceiveMessageSize = 5 * 1024 * 1000);

var app = builder.Build();

if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
    _ = app.Services.GetRequiredService<IOptions<LoginThemeOptions>>().Value;

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseResponseCompression();

app.UseElsaLocalization();
app.UseCors();
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

app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains(HealthCheckExtensions.ElsaTag) && check.Tags.Contains(HealthCheckExtensions.ReadinessTag),
    ResultStatusCodes =
    {
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

var apiEndpointOptions = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value;
var routePrefix = apiEndpointOptions.RoutePrefix;
app.MapWorkflowsApi(routePrefix);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseJsonSerializationErrorHandler();
app.UseWorkflows();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
