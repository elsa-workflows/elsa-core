using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Handlers;
using Elsa.Scheduling.HostedServices;
using Elsa.Scheduling.Services;
using Elsa.Scheduling.TriggerPayloadValidators;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.ShellFeatures;

/// <summary>
/// Provides scheduling features to the system.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Scheduling",
    Description = "Provides CRON-based and scheduled workflow triggering capabilities",
    DependsOn = ["SystemClock", "WorkflowManagement"])]
public class SchedulingFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<UpdateTenantSchedules>()
            .AddSingleton<ITenantActivatedEvent>(sp => sp.GetRequiredService<UpdateTenantSchedules>())
            .AddSingleton<ITenantDeletedEvent>(sp => sp.GetRequiredService<UpdateTenantSchedules>())
            .AddSingleton<IScheduler, LocalScheduler>()
            .AddSingleton<ICronParser, CronosCronParser>()
            .AddSingleton<CronosCronParser>()
            .AddScoped<ITriggerScheduler, DefaultTriggerScheduler>()
            .AddScoped<IBookmarkScheduler, DefaultBookmarkScheduler>()
            .AddScoped<IWorkflowScheduler, DefaultWorkflowScheduler>()
            .AddScoped<DefaultWorkflowScheduler>()
            .AddBackgroundTask<CreateSchedulesBackgroundTask>()
            .AddHandlersFrom<ScheduleWorkflows>()

            //Trigger payload validators.
            .AddTriggerPayloadValidator<CronTriggerPayloadValidator, CronTriggerPayload>();
    }
}
