using Elsa.Persistence.EntityFrameworkCore.Options;
using Elsa.Persistence.Options;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static PersistenceOptions UseEntityFrameworkCoreProvider(this PersistenceOptions configurator, Action<EFCorePersistenceOptions> configure)
    {
        configurator.ElsaOptionsConfigurator.Configure(() => new EFCorePersistenceOptions(configurator), configure);
        return configurator;
    }
}