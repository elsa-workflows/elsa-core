using Elsa.Common;
using Elsa.Extensions;
using Elsa.Persistence.VNext.Extensions.Contracts;
using Elsa.Persistence.VNext.Extensions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.VNext.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceVNext(this IServiceCollection services, Action<PersistenceVNextOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
            services.Configure(configureOptions);

        services.AddOptions<PersistenceVNextOptions>();
        services.TryAddSingleton<IPersistenceSchemaCatalog, DefaultPersistenceSchemaCatalog>();
        services.TryAddSingleton<IPersistenceVNextStatus, DefaultPersistenceVNextStatus>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStartupTask, PersistenceVNextStartupTask>());
        return services;
    }
}
