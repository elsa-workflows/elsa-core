using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Secrets;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Secrets module.
    /// </summary>
    public static IServiceCollection AddSecretsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, SecretsMenu>()
            .AddRemoteApi<ISecretsApi>(backendApiConfig);
    }
}