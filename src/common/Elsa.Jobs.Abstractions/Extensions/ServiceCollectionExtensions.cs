using Elsa.Jobs.Implementations;
using Elsa.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobServices(this IServiceCollection services, IJobSchedulerProvider schedulerProvider, IJobQueueProvider queueProvider)
    {
        services
            .AddSingleton<IJobSerializer, JobSerializer>()
            .AddSingleton<IJobRunner, JobRunner>();
        
        schedulerProvider.ConfigureServices(services);
        queueProvider.ConfigureServices(services);
        return services;
    }
}