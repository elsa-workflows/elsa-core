using Elsa.Studio.Localization.BlazorServer.Services;
using Elsa.Studio.Localization.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Localization.BlazorServer.Extensions;

/// <summary>
/// Provides extension methods for service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the localization module.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="localizationConfig">The localization config.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddLocalizationModule(this IServiceCollection services, LocalizationConfig localizationConfig)
    {
        services.AddControllers();
        services.AddLocalizationModuleCore(localizationConfig);
        services.AddScoped<ICultureService, BlazorServerCultureService>();

        return services;
    }
}