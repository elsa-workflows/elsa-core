using Elsa.Studio.Localization.BlazorWasm.Services;
using Elsa.Studio.Localization.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Localization.BlazorWasm.Extensions;

/// <summary>
/// Provides WebAssembly-specific service registration helpers for localization.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds localization services tailored for the Blazor WebAssembly host.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="localizationConfig">The configuration to apply to localization services.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddLocalizationModule(this IServiceCollection services, LocalizationConfig localizationConfig)
    {
        services.AddLocalizationModuleCore(localizationConfig);

        services.AddScoped<ICultureService, BlazorWasmCultureService>();

        return services;
    }
}