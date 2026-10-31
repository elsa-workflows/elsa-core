using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Runtime.Contracts;
using Elsa.Persistence.VNext.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.VNext.Runtime.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRuntimeEntitiesVNext(this IServiceCollection services, Action<RuntimeEntityOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
            services.Configure(configureOptions);

        services.AddOptions<RuntimeEntityOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPersistenceSchemaProvider, RuntimeEntityPersistenceSchemaProvider>());
        services.TryAddSingleton<RuntimeEntityDefinitionValidator>();
        services.TryAddSingleton<IRuntimeEntityManager, RuntimeEntityManager>();
        return services;
    }
}
