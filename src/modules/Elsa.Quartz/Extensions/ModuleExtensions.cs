using Elsa.Features.Services;
using Elsa.Quartz.Features;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="SchedulingFeature"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs and configures Quartz.NET. Only use this feature if you are not configuring Quartz.NET yourself.
    /// </summary>
    public static IModule UseQuartz(this IModule module, Action<QuartzFeature>? configure = default)
    {
        return module.Use(configure);
    }
    
    /// <summary>
    /// Installs a Quartz.NET implementation for <see cref="IWorkflowScheduler"/>.
    /// </summary>
    public static SchedulingFeature UseQuartzScheduler(this SchedulingFeature feature, Action<QuartzSchedulerFeature>? configure = default)
    {
        feature.Module.Use(configure);
        return feature;
    }
}