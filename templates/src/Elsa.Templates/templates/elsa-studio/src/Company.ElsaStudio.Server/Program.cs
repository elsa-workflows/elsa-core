using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.Abstractions.Models;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Company.ElsaStudio.Server;
using Company.ElsaStudio.Server.Components;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.RootComponents.MaxJSRootComponents = 1000;
    });

var selectedAuthProvider = ConfigureStudioAuthenticationMode(builder.Services, configuration);
var authenticationHandler = ConfigureAuthentication(builder.Services, configuration);
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler
};
var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options)
};

builder.Services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
builder.Services.AddCore().Replace(new(typeof(IBrandingProvider), typeof(StudioBrandingProvider), ServiceLifetime.Scoped));
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddDashboardModule(backendApiConfig);
builder.Services.AddWorkflowsModule();
#if (withLabels)
builder.Services.AddLabelsModule(backendApiConfig);
#endif
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddTranslations();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 5 * 1024 * 1000);
if (selectedAuthProvider != StudioAuthenticationProvider.ElsaLogin)
{
    builder.Services
        .AddAuthenticationUI(configuration.GetSection(LoginThemeOptions.SectionName))
        .AddElsaStudioLoginThemes();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
    app.UseHsts();
}

app.UseElsaLocalization();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<AppHost>()
    .AddInteractiveServerRenderMode();
app.MapFallbackToPage("/_Host");
app.Run();

static Type ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
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
