using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.Themes.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.AI.Extensions;
using Elsa.Studio.Alterations.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Host.Server;
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Studio.Diagnostics.OpenTelemetry.Extensions;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Extensions;
using Elsa.Studio.Secrets.Extensions;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Settings.Extensions;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Translations;
using Elsa.Studio.Workflows.ActivityPickers.Treeview;
using Elsa.Studio.Workflows.Dashboard.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Extensions;
using Elsa.Studio.ExternalAuthentication.BlazorServer.HttpMessageHandlers;
using Elsa.Studio.ExternalAuthentication.Extensions;
using Elsa.Studio.Authentication.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// Build the host.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Per-developer overrides (auth provider, client secrets, backend URL, ...) belong here, not in appsettings.json.
// The file is git-ignored, so the shipped defaults stay runnable out of the box.
configuration.AddJsonFile("appsettings.Local.json", true, true);

// Register Razor services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    // V2 activity wrapper by default.
    options.RootComponents.RegisterCustomElsaStudioElements();

    // To use V1 activity wrapper layout, specify the V1 component instead:
    //options.RootComponents.RegisterCustomElsaStudioElements(typeof(Elsa.Studio.Workflows.Designer.Components.ActivityWrappers.V1.EmbeddedActivityWrapper));

    options.RootComponents.MaxJSRootComponents = 1000;
});

// Choose authentication provider.
// Supported values: "OpenIdConnect", "ElsaIdentity" (default), "ElsaLogin", or "ExternalAuthentication".
var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
    authProvider = "ElsaIdentity";
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
    // Elsa-owned broker. Server is a confidential client; its client secret remains deployment configuration.
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
    ConfigureHttpClientBuilder = options =>
    {
        options.AuthenticationHandler = authenticationHandler;
        options.ConfigureHttpClient = (_, client) =>
        {
            // Set a long time out to simplify debugging both Elsa Studio and the Elsa Server backend.
            client.Timeout = TimeSpan.FromHours(1);
        };
    },
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options =>
    {
        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
        options.SupportedCultures = new[] { options.DefaultCulture }
            .Concat(options.SupportedCultures.Where(culture => culture != options.DefaultCulture) ?? []).ToArray();
    }
};

builder.Services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
builder.Services
    .AddCore(options => configuration.GetSection(StudioThemeOptions.SectionName).Bind(options))
    .Replace(new(typeof(IBrandingProvider), typeof(StudioBrandingProvider), ServiceLifetime.Scoped));
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));
if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
{
    builder.Services
        .AddAuthenticationUI(configuration.GetSection(LoginThemeOptions.SectionName))
        .AddElsaStudioLoginThemes();
}
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddSettingsModule();
builder.Services.AddSecurityModule(backendApiConfig);

// Management UI remains backend-feature-gated. Broker sign-in is active only when selected above.
builder.Services.AddExternalAuthenticationModule(backendApiConfig);

builder.Services.AddDashboardModule(backendApiConfig);
builder.Services.AddWeaverModule(backendApiConfig);
builder.Services.AddWorkflowsModule();
builder.Services.AddWorkflowsDashboardModule();
builder.Services.AddAlterationsModule();
builder.Services.AddOpenTelemetryDiagnosticsModule(backendApiConfig);
builder.Services.AddOpenTelemetryDashboardModule();
builder.Services.AddConsoleLogsModule(backendApiConfig);
builder.Services.AddConsoleLogsDashboardModule();
builder.Services.AddStructuredLogsModule(backendApiConfig);
builder.Services.AddStructuredLogsDashboardModule();
builder.Services.AddSecretsModule(backendApiConfig);
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddTranslations();

// Replace some services with other implementations.
builder.Services.AddScoped<IActivityPickerComponentProvider, TreeviewActivityPickerComponentProvider>();

// Uncomment for the Accordion Activity Picker
//builder.Services.AddScoped<IActivityPickerComponentProvider>(sp => new AccordionActivityPickerComponentProvider
//{
//    // Example - Replace the default category resolver with a custom one.
//    CategoryDisplayResolver = category => category.Split('/').Last().Trim()
//});

// Bind designer options from appsettings.json (e.g. "DesignerOptions:UseReactFlow").
builder.Services.Configure<DesignerOptions>(configuration.GetSection("DesignerOptions"));

// Uncomment for V1 designer theme (default is V2).
// builder.Services.Configure<DesignerOptions>(options =>
// {
//     options.DesignerCssClass = "elsa-flowchart-diagram-designer-v1";
//     options.GraphSettings.Grid.Type = "mesh";
// });

// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize:
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
});

// Build the application.
var app = builder.Build();

if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
    _ = app.Services.GetRequiredService<IOptions<LoginThemeOptions>>().Value;

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseElsaLocalization();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
