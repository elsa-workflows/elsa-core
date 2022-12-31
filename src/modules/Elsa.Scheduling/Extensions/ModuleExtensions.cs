using Elsa.Features.Services;
using Elsa.Scheduling.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseScheduling(this IModule module, Action<SchedulingFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
}