using Elsa.Features.Services;
using Elsa.Scheduling.Features;

namespace Elsa.Scheduling.Extensions;

public static class ModuleExtensions
{
    public static IModule UseScheduling(this IModule module, Action<SchedulingFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
}