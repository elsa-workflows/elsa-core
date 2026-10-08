using Elsa.Studio.Extensions;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Core.BlazorWasm.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services with WASM implementations.
    /// </summary>
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        Action<StudioThemeOptions>? configureTheme = null)
    {
        services.AddSharedServices();
        services.AddCoreInternal(configureTheme);
        
        return services;
    }
}
