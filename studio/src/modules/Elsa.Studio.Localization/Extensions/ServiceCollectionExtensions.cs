using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Translations;

namespace Elsa.Studio.Localization.Extensions;

/// <summary>
/// Provides dependency injection helpers for the localization module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required for localization support in the studio.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="localizationConfig">The localization configuration to apply.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddLocalizationModuleCore(this IServiceCollection services,LocalizationConfig localizationConfig)
    {
        services.AddLocalization();
        services.AddMudTranslations();
        services.Configure(localizationConfig.ConfigureLocalizationOptions);
        services.AddScoped<IFeature, LocalizationFeature>();

        return services;
    }
}
