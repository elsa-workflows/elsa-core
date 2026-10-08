using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
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
    /// <see cref="DefaultBackendApiClientProvider"/> keeps minting clients.
    /// Environments load once from <see cref="EnvironmentAwareFeatureService"/> before
    /// the remote feature catalog is fetched. They are not registered as
    /// <see cref="IStartupTask"/> so Blazor Server host startup does not call the
    /// environments API (or JS interop) during <c>Host.StartAsync</c>.
    /// <see cref="Feature.InitializeAsync"/> only registers the picker.
    /// </remarks>
    public static IServiceCollection AddEnvironmentsModule(this IServiceCollection services, BackendApiConfig? backendApiConfig = null)
    {
        services.AddScoped<IEnvironmentService, DefaultEnvironmentService>();
        services.Replace(ServiceDescriptor.Scoped<IRemoteBackendAccessor, EnvironmentRemoteBackendAccessor>());
        services.AddScoped<EnvironmentLoader>();
        services.AddScoped<IFeature, Feature>();
        services.AddRemoteApi<IEnvironmentsClient>(backendApiConfig);
        services.Replace(ServiceDescriptor.Scoped<IFeatureService>(sp =>
            new EnvironmentAwareFeatureService(
                new DefaultFeatureService(
                    sp.GetServices<IFeature>(),
                    sp.GetRequiredService<IRemoteFeatureProvider>()),
                sp.GetRequiredService<EnvironmentLoader>())));
        return services;
    }
}
