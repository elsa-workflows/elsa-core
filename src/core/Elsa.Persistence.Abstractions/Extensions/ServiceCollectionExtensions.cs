using Elsa.Options;
using Elsa.Persistence.Options;

namespace Elsa.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static ElsaOptionsConfigurator ConfigurePersistence(this ElsaOptionsConfigurator configurator, Action<PersistenceOptions>? configure = default)
    {
        configurator.Configure(() => new PersistenceOptions(configurator), configure);
        return configurator;
    }
}