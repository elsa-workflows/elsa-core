using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Menu;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.ExternalAuthentication.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalAuthenticationModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<ISecurityMenuContributor, ExternalAuthenticationSecurityMenuContributor>()
            .AddScoped<IExternalAuthenticationPermissionService, ExternalAuthenticationPermissionService>()
            .AddSingleton<ICustomConnectionEditorRegistry, CustomConnectionEditorRegistry>()
            .AddRemoteApi<IExternalAuthenticationConnectionsApi>(backendApiConfig)
            .AddRemoteApi<IIdentityRolesApi>(backendApiConfig)
            .AddRemoteApi<IExternalIdentityLinksApi>(backendApiConfig)
            .AddRemoteApi<IExternalAuthenticationOperationsApi>(backendApiConfig)
            .AddRemoteApi<ILoginMethodsApi>(backendApiConfig)
            .AddRemoteApi<IExternalAuthenticationBrokerApi>(backendApiConfig);
    }
}
