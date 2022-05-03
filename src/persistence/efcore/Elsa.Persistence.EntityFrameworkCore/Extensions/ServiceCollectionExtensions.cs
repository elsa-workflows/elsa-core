using Elsa.Persistence.EntityFrameworkCore.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Options;
using Elsa.Persistence.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static PersistenceOptions UseEntityFrameworkCoreProvider(this PersistenceOptions configurator, Action<EFCorePersistenceOptions> configure)
    {
        configurator.ElsaOptionsConfigurator.Configure(() => new EFCorePersistenceOptions(configurator), configure);
        return configurator;
    }

    public static IServiceCollection AutoRunMigrations(this IServiceCollection services)
    {
        return services.AddHostedService<RunMigrations>();
    }
}