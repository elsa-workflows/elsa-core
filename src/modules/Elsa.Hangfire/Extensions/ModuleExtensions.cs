using Elsa.Features.Services;
using Elsa.Hangfire.Features;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Features;
using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="HangfireFeature"/>.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Installs and configures Hangfire. Only use this feature if you are not configuring Hangfire yourself.
    /// </summary>
    public static IModule UseHangfire(this IModule module, Action<HangfireFeature>? configure = default)
    {
        return module.Use(configure);
    }
    
    /// <summary>
    /// Configures Hangfire to use SQL Server storage. Only use this feature if you are not configuring Hangfire yourself.
    /// </summary>
    public static HangfireFeature UseSqlServerStorage(this HangfireFeature feature, Action<HangfireSqlServerStorageFeature> configure)
    {
        feature.Module.Use(configure);
        return feature;
    }
    
    /// <summary>
    /// Configures Hangfire to use SQLite storage. Only use this feature if you are not configuring Hangfire yourself.
    /// </summary>
    public static HangfireFeature UseSqliteStorage(this HangfireFeature feature, Action<HangfireSqliteStorageFeature> configure)
    {
        feature.Module.Use(configure);
        return feature;
    }
    
    /// <summary>
    /// Installs a Hangfire implementation for <see cref="IWorkflowScheduler"/>.
    /// </summary>
    public static SchedulingFeature UseHangfireScheduler(this SchedulingFeature feature, Action<HangfireSchedulerFeature>? configure = default)
    {
        feature.Module.Use(configure);
        return feature;
    }
    
    /// <summary>
    /// Installs a Hangfire implementation for <see cref="IWorkflowScheduler"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseHangfireBackgroundActivityScheduler(this WorkflowRuntimeFeature feature, Action<HangfireBackgroundActivitySchedulerFeature>? configure = default)
    {
        feature.Module.Use(configure);
        return feature;
    }
}