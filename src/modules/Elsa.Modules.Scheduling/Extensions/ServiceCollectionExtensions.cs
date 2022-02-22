using Elsa.Mediator.Extensions;
using Elsa.Modules.Scheduling.Contracts;
using Elsa.Modules.Scheduling.Handlers;
using Elsa.Modules.Scheduling.HostedServices;
using Elsa.Modules.Scheduling.Jobs;
using Elsa.Modules.Scheduling.Services;
using Elsa.Scheduling.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.Scheduling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingActivities(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkflowTriggerScheduler, WorkflowTriggerScheduler>()
            .AddSingleton<IWorkflowBookmarkScheduler, WorkflowBookmarkScheduler>()
            .AddJobHandler<RunWorkflowJobHandler>()
            .AddJobHandler<ResumeWorkflowJobHandler>()
            .AddNotificationHandlersFrom<ScheduleWorkflows>()
            .AddHostedService<ScheduleWorkflowsHostedService>();

        return services;
    }
}