using Elsa.Labels.Configuration;
using Elsa.Labels.EntityFrameworkCore.Options;

namespace Elsa.Labels.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static LabelPersistenceOptions UseEntityFrameworkCore(this LabelPersistenceOptions configurator, Action<EFCoreLabelPersistenceConfigurator> configure)
    {
        configurator.ServiceConfiguration.Configure(() => new EFCoreLabelPersistenceConfigurator(configurator), configure);
        return configurator;
    }
}