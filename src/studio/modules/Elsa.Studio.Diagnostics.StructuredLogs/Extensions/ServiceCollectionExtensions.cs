using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Diagnostics.StructuredLogs.Client;
using Elsa.Studio.Diagnostics.StructuredLogs.Contracts;
using Elsa.Studio.Diagnostics.StructuredLogs.Menu;
using Elsa.Studio.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Extensions;

/// <summary>
/// Contains extension methods for the structured logs module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the structured logs module.
    /// </summary>
    public static IServiceCollection AddStructuredLogsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, StructuredLogsMenu>()
            .AddScoped<IStructuredLogService, RemoteStructuredLogService>()
            .AddScoped<IStructuredLogObserver, SignalRStructuredLogObserver>()
            .AddRemoteApi<IStructuredLogsApi>(backendApiConfig);
    }
}
