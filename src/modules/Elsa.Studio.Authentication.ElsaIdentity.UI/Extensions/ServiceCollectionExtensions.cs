using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Services;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;

/// <summary>
/// Service registration extensions for the ElsaIdentity UI module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Elsa Identity login UI (route: <c>/login</c>), an unauthorized redirect behavior and the app bar sign-out menu.
    /// </summary>
    public static IServiceCollection AddElsaIdentityUI(this IServiceCollection services)
    {
        services.AddScoped<IFeature, ElsaIdentityUIFeature>();
        services.AddSingleton(new StudioAuthenticationProviderRegistration(StudioAuthenticationProvider.ElsaIdentity));
        services.AddScoped<ILoginMethodCatalog, ElsaIdentityLoginMethodCatalog>();
        services.AddScoped<ILoginMethodComponentProvider, ElsaIdentityLoginMethodComponentProvider>();
        services.AddSingleton<ILoginMethodIconProvider, ElsaIdentityLoginMethodIconProvider>();
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<RedirectToLogin>>();

        return services;
    }
}
