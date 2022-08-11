using Elsa.Features.Services;
using Elsa.Jobs.Activities.Features;

namespace Elsa.Jobs.Activities.Extensions;

public static class ModuleExtensions
{
    public static IModule UseJobActivities(this IModule module, Action<JobActivitiesFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
}