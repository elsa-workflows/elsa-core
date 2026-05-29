using Elsa.Common.Features;
using Elsa.Common.Multitenancy;
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Handlers;
using Elsa.Scheduling.Services;
using Elsa.Scheduling.StartupTasks;
using Elsa.Scheduling.TriggerPayloadValidators;
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
            .AddSingleton<UpdateTenantSchedules>()
            .AddSingleton<ITenantDeletedEvent>(sp => sp.GetRequiredService<UpdateTenantSchedules>())
            .AddSingleton<IScheduler, LocalScheduler>()
            .AddSingleton<CronosCronParser>()
            .AddSingleton(CronParser)
            .AddScoped<ITriggerScheduler, DefaultTriggerScheduler>()
            .AddScoped<IBookmarkScheduler, DefaultBookmarkScheduler>()
            .AddScoped<DefaultWorkflowScheduler>()
            .AddScoped(WorkflowScheduler)
            .AddStartupTask<CreateSchedulesStartupTask>()
            .AddHandlersFrom<ScheduleWorkflows>()

            //Trigger payload validators.
            .AddTriggerPayloadValidator<CronTriggerPayloadValidator, CronTriggerPayload>()

            // Graceful shutdown: register scheduled-trigger ingress for diagnostic visibility (FR-006).
            .AddSingleton<Elsa.Workflows.Runtime.IIngressSource, Elsa.Scheduling.IngressSources.ScheduledTriggerIngressSource>();

        Services.Configure<ExpressionOptions>(options =>
        {
            options.AddTypeAlias<CronBookmarkPayload>();
            options.AddTypeAlias<CronTriggerPayload>();
            options.AddTypeAlias<DelayPayload>();
            options.AddTypeAlias<StartAtPayload>();
            options.AddTypeAlias<TimerBookmarkPayload>();
            options.AddTypeAlias<TimerTriggerPayload>();
        });

        Module.Configure<WorkflowManagementFeature>(management => management.AddActivitiesFrom<SchedulingFeature>());
    }
}
