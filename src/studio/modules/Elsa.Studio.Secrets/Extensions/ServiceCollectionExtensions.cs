using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.Handlers;
using Elsa.Studio.Secrets.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Secrets.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, SecretsMenu>()
            .AddUIHintHandler<SecretPickerHandler>()
            .AddRemoteApi<ISecretsApi>(backendApiConfig);
    }
}
