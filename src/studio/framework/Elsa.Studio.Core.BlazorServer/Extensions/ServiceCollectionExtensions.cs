using Elsa.Studio.Core.BlazorServer.HostedServices;
using Elsa.Studio.Extensions;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Core.BlazorServer.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services with Blazor Server implementations.
    /// </summary>
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        Action<StudioThemeOptions>? configureTheme = null)
    {
        services.AddCoreInternal(configureTheme);
        services.AddSharedServices();
        services.AddHostedService<RunStartupTasksHostedService>();
        
        return services;
    }
}
