using Elsa.Labels.EntityFrameworkCore.Options;
using Elsa.Labels.Options;
using Elsa.Workflows.Persistence.Options;

namespace Elsa.Labels.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static LabelPersistenceOptions UseEntityFrameworkCoreProvider(this LabelPersistenceOptions configurator, Action<EFCoreLabelPersistenceOptions> configure)
    {
        configurator.ElsaOptionsConfigurator.Configure(() => new EFCoreLabelPersistenceOptions(configurator), configure);
        return configurator;
    }
}