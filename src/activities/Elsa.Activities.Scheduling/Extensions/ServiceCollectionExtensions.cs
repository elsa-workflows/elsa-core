using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Handlers;
using Elsa.Activities.Scheduling.HostedServices;
using Elsa.Activities.Scheduling.Jobs;
using Elsa.Activities.Scheduling.Services;
using Elsa.Mediator.Extensions;
using Elsa.Scheduling.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Scheduling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingActivities(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkflowTriggerScheduler, WorkflowTriggerScheduler>()
            .AddJobHandler<RunWorkflowJobHandler>()
            .AddJobHandler<ResumeWorkflowJobHandler>()
            .AddNotificationHandlersFrom<ScheduleWorkflowsHandler>()
            .AddHostedService<ScheduleWorkflowsHostedService>();

        return services;
    }
}