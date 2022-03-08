using Elsa.Jobs.Contracts;
using Elsa.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services, IJobSchedulerProvider schedulerProvider, IJobQueueProvider queueProvider)
    {
        services
            .AddSingleton<IJobSerializer, JobSerializer>()
            .AddSingleton<IJobRunner, JobRunner>();
        
        schedulerProvider.ConfigureServices(services);
        queueProvider.ConfigureServices(services);
        return services;
    }

    public static IServiceCollection AddJobHandler<T>(this IServiceCollection services) where T : class, IJobHandler => services.AddSingleton<IJobHandler, T>();
}