using Elsa.Labels.Configuration;
using Elsa.Labels.EntityFrameworkCore.Configuration;

namespace Elsa.Labels.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static LabelsConfigurator UseEntityFrameworkCore(this LabelsConfigurator configurator, Action<EFCoreLabelPersistenceConfigurator> configure)
    {
        configurator.ServiceConfiguration.Configure(configure);
        return configurator;
    }
}