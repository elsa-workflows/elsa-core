using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Secrets.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the optional Elsa Secrets bridge for External Authentication.
    /// </summary>
    public static IServiceCollection AddElsaSecretsExternalAuthentication(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISecretBindingResolver, ElsaSecretBindingResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IManagedSecretBindingWriter, ElsaSecretBindingResolver>());
        return services;
    }
}
