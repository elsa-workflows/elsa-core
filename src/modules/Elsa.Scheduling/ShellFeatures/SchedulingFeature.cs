using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Common.ShellFeatures;
using Elsa.Extensions;
using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Handlers;
using Elsa.Scheduling.HostedServices;
using Elsa.Scheduling.Services;
using Elsa.Scheduling.TriggerPayloadValidators;
using Elsa.Workflows.Management.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.ShellFeatures;

/// <summary>
/// Provides scheduling features to the system.
/// </summary>
[ShellFeature(
    DisplayName = "Scheduling",
    Description = "Provides scheduling capabilities for workflows including cron and delay-based triggers",
    DependsOn = [typeof(SystemClockFeature)])]
[UsedImplicitly]
public class SchedulingFeature : IShellFeature
{
    /// <summary>
    /// Gets or sets the trigger scheduler factory.
    /// </summary>
    public Func<IServiceProvider, IWorkflowScheduler> WorkflowScheduler { get; set; } = sp => sp.GetRequiredService<DefaultWorkflowScheduler>();

    /// <summary>
    /// Gets or sets the CRON parser factory.
    /// </summary>
    public Func<IServiceProvider, ICronParser> CronParser { get; set; } = sp => sp.GetRequiredService<CronosCronParser>();

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<UpdateTenantSchedules>()
            .AddSingleton<ITenantActivatedEvent>(sp => sp.GetRequiredService<UpdateTenantSchedules>())
            .AddSingleton<ITenantDeletedEvent>(sp => sp.GetRequiredService<UpdateTenantSchedules>())
            .AddSingleton<IScheduler, LocalScheduler>()
            .AddSingleton<CronosCronParser>()
            .AddSingleton(CronParser)
            .AddScoped<ITriggerScheduler, DefaultTriggerScheduler>()
            .AddScoped<IBookmarkScheduler, DefaultBookmarkScheduler>()
            .AddScoped<DefaultWorkflowScheduler>()
            .AddScoped(WorkflowScheduler)
            .AddBackgroundTask<CreateSchedulesBackgroundTask>()
            .AddHandlersFrom<ScheduleWorkflows>()
            .AddTriggerPayloadValidator<CronTriggerPayloadValidator, CronTriggerPayload>()
            .AddActivitiesFrom<SchedulingFeature>();
    }
}

