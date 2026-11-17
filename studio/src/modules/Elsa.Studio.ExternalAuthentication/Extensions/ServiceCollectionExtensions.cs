using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Options;
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
        var brokerClientOptions = new ElsaClientBuilderOptions();
        backendApiConfig.ConfigureHttpClientBuilder?.Invoke(brokerClientOptions);

        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<ISecurityMenuContributor, ExternalAuthenticationSecurityMenuContributor>()
            .AddScoped<IExternalAuthenticationPermissionService, ExternalAuthenticationPermissionService>()
            .AddSingleton<ICustomConnectionEditorRegistry, CustomConnectionEditorRegistry>()
            .AddRemoteApi<IExternalAuthenticationConnectionManagementApi>(backendApiConfig)
            .AddRemoteApi<IIdentityRolesApi>(backendApiConfig)
            .AddRemoteApi<IExternalIdentityLinkManagementApi>(backendApiConfig)
            .AddRemoteApi<IExternalAuthenticationOperationsApi>(backendApiConfig)
            .AddRemoteApi<ILoginMethodsApi>(backendApiConfig)
            // Register the broker's named client without the Studio bearer handler. Refresh and sign-in use this client to acquire their tokens.
            .AddApi<IExternalAuthenticationBrokerApi>(brokerClientOptions);
    }
}
