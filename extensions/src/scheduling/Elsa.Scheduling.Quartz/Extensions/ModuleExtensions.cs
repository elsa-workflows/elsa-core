using Elsa.Features.Services;
using Elsa.Scheduling.Quartz.Features;
using Elsa.Scheduling.Features;
using Elsa.Scheduling;
using Elsa.Workflows.Runtime.Features;

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
    public static IModule UseQuartz(this IModule module, Action<QuartzFeature>? configure = null)
    {
        return module.Use(configure);
    }

    /// <summary>
    /// Installs a Quartz.NET implementation for <see cref="IWorkflowScheduler"/>.
    /// Registers a tenant-aware Quartz job factory when the container has no host-provided
    /// <c>IJobFactory</c>. Quartz's own default <c>MicrosoftDependencyInjectionJobFactory</c>
    /// is treated as replaceable. Hosts that register their own factory must resolve jobs
    /// inside the trigger's tenant, or the job fails instead of using the default tenant.
    /// </summary>
    public static SchedulingFeature UseQuartzScheduler(this SchedulingFeature feature, Action<QuartzSchedulerFeature>? configure = null)
    {
        feature.Module.Use(configure);
        return feature;
    }

    /// <summary>
    /// Installs a Quartz.NET implementation for <see cref="Elsa.Workflows.Runtime.IBackgroundActivityScheduler"/>.
    /// Registers a tenant-aware Quartz job factory when the container has no host-provided
    /// <c>IJobFactory</c>. Quartz's own default factory is treated as replaceable.
    /// </summary>
    public static WorkflowRuntimeFeature UseQuartzBackgroundActivityScheduler(this WorkflowRuntimeFeature feature, Action<QuartzBackgroundActivitySchedulerFeature>? configure = null)
    {
        feature.Module.Use(configure);
        return feature;
    }
}