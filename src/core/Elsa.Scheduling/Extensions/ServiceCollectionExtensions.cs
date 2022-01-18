using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScheduling(this IServiceCollection services, ISchedulingServiceProvider serviceProvider)
    {
        services.AddSingleton<IJobManager, JobManager>();
        
        serviceProvider.ConfigureServices(services);
        return services;
    }

    public static IServiceCollection AddJobHandler<T>(this IServiceCollection services) where T : class, IJobHandler => services.AddSingleton<IJobHandler, T>();
}