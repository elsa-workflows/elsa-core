using Elsa.Mediator.Extensions;
using Elsa.Scheduling.Implementations;
using Elsa.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSchedulingServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkflowTriggerScheduler, WorkflowTriggerScheduler>()
            .AddSingleton<IWorkflowBookmarkScheduler, WorkflowBookmarkScheduler>()
            .AddNotificationHandlersFrom<HostedServices.ScheduleWorkflows>()
            .AddHostedService<HostedServices.ScheduleWorkflows>();

        return services;
    }
}