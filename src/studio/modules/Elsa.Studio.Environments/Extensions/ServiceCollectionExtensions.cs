using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Environments.Tasks;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.Environments.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the environments module.
    /// </summary>
    /// <remarks>
    /// Environment switching updates <see cref="IRemoteBackendAccessor"/> so
    /// <see cref="Elsa.Studio.Services.DefaultBackendApiClientProvider"/> keeps minting clients.
    /// </remarks>
    public static IServiceCollection AddEnvironmentsModule(this IServiceCollection services, BackendApiConfig? backendApiConfig = null)
    {
        services.AddScoped<IEnvironmentService, DefaultEnvironmentService>();
        services.Replace(ServiceDescriptor.Scoped<IRemoteBackendAccessor, EnvironmentRemoteBackendAccessor>());
        services.AddScoped<LoadEnvironmentsStartupTask>();
        services.AddScoped<IStartupTask>(sp => sp.GetRequiredService<LoadEnvironmentsStartupTask>());
        services.AddScoped<IFeature, Feature>();
        services.AddRemoteApi<IEnvironmentsClient>(backendApiConfig);
        return services;
    }
}
