using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Scheduling.Handlers;
using Elsa.Scheduling.HostedServices;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Features;

/// <summary>
/// Provides scheduling features to the system.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
public class SchedulingFeature : FeatureBase
{
    /// <inheritdoc />
    public SchedulingFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// Gets or sets the trigger scheduler.
    /// </summary>
    public Func<IServiceProvider, IWorkflowScheduler> WorkflowScheduler { get; set; } = sp => sp.GetRequiredService<DefaultWorkflowScheduler>();
    
    /// <summary>
    /// Gets or sets the CRON parser.
    /// </summary>
    public Func<IServiceProvider, ICronParser> CronParser { get; set; } = sp => sp.GetRequiredService<CronosCronParser>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<IScheduler, LocalScheduler>()
            .AddSingleton<CronosCronParser>()
            .AddSingleton(CronParser)
            .AddScoped<ITriggerScheduler, DefaultTriggerScheduler>()
            .AddScoped<IBookmarkScheduler, DefaultBookmarkScheduler>()
            .AddSingleton<IScheduler, LocalScheduler>()
            .AddScoped<DefaultWorkflowScheduler>()
            .AddSingleton<CronosCronParser>()
            .AddSingleton(CronParser)
            .AddScoped(WorkflowScheduler)
            .AddBackgroundTask<CreateSchedulesBackgroundTask>()
            .AddHandlersFrom<ScheduleWorkflows>();

        Module.Configure<WorkflowManagementFeature>(management => management.AddActivitiesFrom<SchedulingFeature>());
    }
}