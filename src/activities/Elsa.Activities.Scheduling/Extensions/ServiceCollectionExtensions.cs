using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Handlers;
using Elsa.Activities.Scheduling.Jobs;
using Elsa.Activities.Scheduling.Services;
using Elsa.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Scheduling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingServices(this IServiceCollection services, ISchedulingServiceProvider serviceProvider)
    {
        services
            .AddSingleton<IJobManager, JobManager>()
            .AddSingleton<IJobHandler, RunWorkflowJobHandler>()
            .AddSingleton<IJobHandler, ResumeWorkflowJobHandler>()
            .AddNotificationHandlersFrom<ScheduleWorkflowsHandler>();
        
        serviceProvider.ConfigureServices(services);
        return services;
    }
}