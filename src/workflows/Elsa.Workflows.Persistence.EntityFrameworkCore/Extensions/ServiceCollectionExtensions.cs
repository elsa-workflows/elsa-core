using Elsa.Persistence.Options;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Options;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static PersistenceOptions UseEntityFrameworkCoreProvider(this PersistenceOptions configurator, Action<EFCorePersistenceOptions> configure)
    {
        configurator.ElsaOptionsConfigurator.Configure(() => new EFCorePersistenceOptions(configurator), configure);
        return configurator;
    }
}