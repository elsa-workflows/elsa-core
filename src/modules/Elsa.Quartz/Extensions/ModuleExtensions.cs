using Elsa.Features.Services;
using Elsa.Quartz.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseQuartz(this IModule module, Action<QuartzFeature>? configure = default)
    {
        module.Use(configure);
        return module;
    }
}