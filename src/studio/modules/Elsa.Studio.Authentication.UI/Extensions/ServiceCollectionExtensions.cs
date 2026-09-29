using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.UI.Components.Themes;
using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.Options;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationUI(this IServiceCollection services)
        => services.AddAuthenticationUI(loginThemeConfiguration: null);

    /// <summary>
    /// Adds the shared authentication UI and its startup-selected login theme framework.
    /// </summary>
    public static IServiceCollection AddAuthenticationUI(
        this IServiceCollection services,
        IConfigurationSection? loginThemeConfiguration)
    {
        services.TryAddScoped<ILoginMethodComponentRegistry, LoginMethodComponentRegistry>();
        services.TryAddScoped<ILoginMethodIconRegistry, LoginMethodIconRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IFeature, Feature>());

        var options = services.AddOptions<LoginThemeOptions>();
        if (loginThemeConfiguration is not null)
            options.Bind(loginThemeConfiguration);
        options.ValidateOnStart();
        services.AddOptions<StudioThemeOptions>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<LoginThemeOptions>, LoginThemeOptionsValidator>());
        services.TryAddScoped<ILoginThemeRegistry, LoginThemeRegistry>();
        services.TryAddScoped<LoginThemeContextFactory>();

        if (!services.Any(x => x.ServiceType == typeof(AuthenticationUiRegistrationMarker)))
        {
            services.AddSingleton<AuthenticationUiRegistrationMarker>();
            services
                .AddLoginTheme<ClassicUnifiedLoginTheme>(LoginThemeIds.Classic)
                .AddLoginTheme<ClassicUnifiedLoginTheme>(LoginThemeIds.ClassicUnified)
                .AddLoginTheme<ClassicRefinedSplitLoginTheme>(LoginThemeIds.ClassicRefinedSplit)
                .AddLoginTheme<ClassicBrandCanvasLoginTheme>(LoginThemeIds.ClassicBrandCanvas);
        }

        return services;
    }

    /// <summary>
    /// Registers a component-backed login theme under a stable deployment-facing identifier.
    /// </summary>
    public static IServiceCollection AddLoginTheme<TComponent>(this IServiceCollection services, string id)
        where TComponent : LoginThemeComponentBase
    {
        services.TryAddScoped<ComponentLoginThemeProvider<TComponent>>();
        return services.AddLoginThemeRegistration(id, typeof(ComponentLoginThemeProvider<TComponent>));
    }

    /// <summary>
    /// Registers an advanced provider-backed login theme under a stable deployment-facing identifier.
    /// </summary>
    public static IServiceCollection AddLoginThemeProvider<TProvider>(this IServiceCollection services, string id)
        where TProvider : class, ILoginThemeProvider
    {
        services.TryAddScoped<TProvider>();
        return services.AddLoginThemeRegistration(id, typeof(TProvider));
    }

    private static IServiceCollection AddLoginThemeRegistration(this IServiceCollection services, string id, Type providerType)
    {
        services.Add(ServiceDescriptor.Singleton(new LoginThemeRegistration(id, providerType)));
        return services;
    }

    private sealed class AuthenticationUiRegistrationMarker;
}
