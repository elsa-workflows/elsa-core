using Elsa.Features.Extensions;
using Elsa.Features.Services;
using Elsa.Quartz.Features;

namespace Elsa.Quartz.Extensions;

public static class ModuleExtensions
{
    public static IModule UseQuartz(this IModule module, Action<QuartzFeature>? configure = default)
    {
        module.Use(configure);
        return module;
    }
}