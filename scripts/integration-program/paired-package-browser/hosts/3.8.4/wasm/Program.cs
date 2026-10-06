using Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.Themes.Extensions;
using Elsa.Studio.AI.Extensions;
using Elsa.Studio.Alterations.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.OpenTelemetry.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Extensions;
using Elsa.Studio.Secrets.Extensions;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Settings.Extensions;
using Elsa.Studio.Workflows.Dashboard.Extensions;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Extensions;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.HttpMessageHandlers;
using Elsa.Studio.ExternalAuthentication.Extensions;
using Elsa.Studio.Authentication.Abstractions.Models;
using Microsoft.Extensions.Options;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;
var services = builder.Services;

// Register root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Choose authentication provider.
// Supported values: "OpenIdConnect" (default), "ElsaIdentity", "ElsaLogin", or "ExternalAuthentication".
var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
    authProvider = "OpenIdConnect";
if (!Enum.TryParse<StudioAuthenticationProvider>(authProvider, true, out var selectedAuthProvider))
    throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'. Supported values are 'OpenIdConnect', 'ElsaIdentity', 'ElsaLogin', and 'ExternalAuthentication'.");
builder.Services.AddStudioAuthenticationMode(options => options.Provider = selectedAuthProvider);

Type authenticationHandler;

if (authProvider.Equals("ElsaIdentity", StringComparison.OrdinalIgnoreCase))
{
    // Elsa Identity (username/password against Elsa backend) + login UI at /login.
    builder.Services.AddElsaIdentity();
    builder.Services.AddElsaIdentityUI();
    authenticationHandler = typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler);
}
else if (authProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
{
    // OpenID Connect.
    builder.Services.AddOpenIdConnectAuth(options =>
    {
        configuration.GetSection("Authentication:OpenIdConnect").Bind(options);

        // If you see a 401 from the OIDC handler while calling the "userinfo" endpoint,
        // either disable UserInfo retrieval (recommended for most setups), or configure your IdP/app registration
        // to allow calling userinfo with the issued access token.
        // options.GetClaimsFromUserInfoEndpoint = false;
    });
    authenticationHandler = typeof(OidcAuthenticatingApiHttpMessageHandler);
}
else if (authProvider.Equals("ElsaLogin", StringComparison.OrdinalIgnoreCase))
{
    // Legacy Elsa Login (username/password against Elsa backend) + login UI at /login.
    builder.Services.AddLoginModule().UseElsaIdentity();
    authenticationHandler = typeof(AuthenticatingApiHttpMessageHandler);
}
else if (authProvider.Equals("ExternalAuthentication", StringComparison.OrdinalIgnoreCase))
{
    // Elsa-owned broker. WebAssembly is a public client: no client secret is accepted.
    builder.Services.AddExternalAuthenticationBroker(options =>
        configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
    authenticationHandler = typeof(ExternalAuthenticationAuthenticatingApiHttpMessageHandler);
}
else
{
    throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'. Supported values are 'OpenIdConnect', 'ElsaIdentity', 'ElsaLogin', and 'ExternalAuthentication'.");
}

// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler,
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options),
};

services.AddCore(options => configuration.GetSection(StudioThemeOptions.SectionName).Bind(options));
services.AddShell();
if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
{
    services
        .AddAuthenticationUI(configuration.GetSection(LoginThemeOptions.SectionName))
        .AddElsaStudioLoginThemes();
}
services.AddRemoteBackend(backendApiConfig);
services.AddSettingsModule();
services.AddSecurityModule(backendApiConfig);

// Management UI remains feature-gated by the Elsa backend; broker login is activated only by the provider above.
services.AddExternalAuthenticationModule(backendApiConfig);

services.AddDashboardModule(backendApiConfig);
services.AddWeaverModule(backendApiConfig);
services.AddWorkflowsModule();
services.AddWorkflowsDashboardModule();
services.AddAlterationsModule();
services.AddOpenTelemetryDiagnosticsModule(backendApiConfig);
services.AddOpenTelemetryDashboardModule();
services.AddConsoleLogsModule(backendApiConfig);
services.AddConsoleLogsDashboardModule();
services.AddStructuredLogsModule(backendApiConfig);
services.AddStructuredLogsDashboardModule();
services.AddSecretsModule(backendApiConfig);
services.AddLocalizationModule(localizationConfig);

// Build the application.
var app = builder.Build();

if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
    _ = app.Services.GetRequiredService<IOptions<LoginThemeOptions>>().Value;

await app.UseElsaLocalization();

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Run the application.
await app.RunAsync();
