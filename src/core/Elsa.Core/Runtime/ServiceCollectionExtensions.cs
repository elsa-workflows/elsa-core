using System;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStartupRunner(this IServiceCollection services)
        {
            return services
                .AddTransient<IStartupRunner, StartupRunner>()
                .AddHostedService<StartupRunnerHostedService>();
        }

        public static IServiceCollection AddStartupTask<TStartupTask>(this IServiceCollection services) where TStartupTask : class, IStartupTask
        {
            return services
                .AddTransient<IStartupTask, TStartupTask>()
                .AddTransient<TStartupTask>();
        }
        
        public static IServiceCollection AddStartupTask<TStartupTask>(this IServiceCollection services, Func<IServiceProvider, TStartupTask> factory) where TStartupTask : class, IStartupTask
        {
            return services
                .AddTransient<IStartupTask, TStartupTask>(factory)
                .AddTransient<TStartupTask>();
        }
    }
}