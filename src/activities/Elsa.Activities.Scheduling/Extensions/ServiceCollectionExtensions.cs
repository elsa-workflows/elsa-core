using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Handlers;
using Elsa.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Scheduling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulingServices(this IServiceCollection services, ISchedulingServiceProvider serviceProvider)
    {
        services.AddNotificationHandlersFrom<ScheduleWorkflowsHandler>();
        serviceProvider.ConfigureServices(services);
        return services;
    }
}