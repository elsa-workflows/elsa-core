using Elsa.Jobs.Contracts;
using Elsa.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services, ISchedulingServiceProvider serviceProvider)
    {
        services.AddSingleton<IJobRunner, JobRunner>();
        
        serviceProvider.ConfigureServices(services);
        return services;
    }

    public static IServiceCollection AddJobHandler<T>(this IServiceCollection services) where T : class, IJobHandler => services.AddSingleton<IJobHandler, T>();
}