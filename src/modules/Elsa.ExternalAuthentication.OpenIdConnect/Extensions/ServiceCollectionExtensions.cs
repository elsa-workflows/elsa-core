using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.OpenIdConnect.Services;
using Elsa.ExternalAuthentication.OpenIdConnect.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the startup-installed OpenID Connect protocol adapter for External Authentication.
    /// </summary>
    public static IServiceCollection AddOpenIdConnectExternalAuthentication(this IServiceCollection services)
    {
        services.AddExternalAuthenticationExtension(ExternalAuthenticationExtensionKind.Adapter, OpenIdConnectExternalAuthenticationAdapter.AdapterType);
        services.TryAddSingleton<IProviderHttpClient>(serviceProvider => serviceProvider.GetRequiredService<IProviderHttpClientFactory>().CreateClient());
        services.TryAddSingleton<OpenIdConnectSettingsParser>();
        services.TryAddSingleton<OpenIdConnectExternalAuthenticationAdapter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExternalAuthenticationAdapter, OpenIdConnectExternalAuthenticationAdapter>());
        return services;
    }
}
