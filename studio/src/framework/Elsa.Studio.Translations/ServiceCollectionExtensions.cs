using Elsa.Studio.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Translations;

/// <summary>
/// Provides extension methods for service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the translations.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddTranslations(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationProvider, ResxLocalizationProvider>();
        return services;
    }
}