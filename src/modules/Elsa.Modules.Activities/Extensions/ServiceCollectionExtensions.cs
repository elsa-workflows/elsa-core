using Elsa.Modules.Activities.Options;
using Elsa.Options;

namespace Elsa.Modules.Activities.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services for activities provided by this package.
    /// </summary>
    public static ActivityOptions ConfigureCoreActivityServices(this ElsaOptionsConfigurator configurator)
    {
        return configurator.Configure<ActivityOptions>();
    }
}