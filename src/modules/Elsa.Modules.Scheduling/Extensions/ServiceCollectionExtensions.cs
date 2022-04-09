using Elsa.Mediator.Extensions;
using Elsa.Modules.Scheduling.Contracts;
using Elsa.Modules.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;
using ScheduleWorkflows = Elsa.Modules.Scheduling.HostedServices.ScheduleWorkflows;

namespace Elsa.Modules.Scheduling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkflowTriggerScheduler, WorkflowTriggerScheduler>()
            .AddSingleton<IWorkflowBookmarkScheduler, WorkflowBookmarkScheduler>()
            .AddNotificationHandlersFrom<ScheduleWorkflows>()
            .AddHostedService<ScheduleWorkflows>();

        return services;
    }
}