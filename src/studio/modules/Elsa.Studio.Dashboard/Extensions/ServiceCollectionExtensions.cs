using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Client;
using Elsa.Studio.Dashboard.Menu;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.Dashboard.Extensions;

/// <summary>
/// Provides extension methods for configuring dashboard services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the dashboard module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        services.TryAddSingleton<IDashboardWidgetRegistry, DashboardWidgetRegistry>();

        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, DashboardMenu>()
            .AddScoped<IDashboardService, DashboardService>();
    }

    /// <summary>
    /// Adds the dashboard module services with remote backend dashboard API support.
    /// </summary>
    public static IServiceCollection AddDashboardModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddDashboardModule()
            .AddRemoteApi<IDashboardApi>(backendApiConfig);
    }
}
