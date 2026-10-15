using Elsa.Persistence.VNext.Contracts;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Persistence.VNext.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Secrets.Persistence.VNext.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretsPersistenceVNext(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPersistenceSchemaProvider, SecretPersistenceSchemaProvider>());
        services.Replace(ServiceDescriptor.Singleton<ISecretRepository, VNextSecretRepository>());
        return services;
    }
}
