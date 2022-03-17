using System;
using Elsa.HostedServices;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStartupRunner(this IServiceCollection services)
        {
            return services
                .AddScoped<IStartupRunner, StartupRunner>()
                .AddScoped<ISharedStartupRunner, SharedStartupRunner>()
                .AddHostedService<StartupRunnerHostedService>();
        }

        public static IServiceCollection AddStartupTask<TStartupTask>(this IServiceCollection services) where TStartupTask : class, IStartupTask
        {
            return services
                .AddScoped<TStartupTask>()
                .AddScoped<IStartupTask, TStartupTask>(sp => sp.GetRequiredService<TStartupTask>());
        }

        public static IServiceCollection AddStartupTask<TStartupTask>(this IServiceCollection services, Func<IServiceProvider, TStartupTask> factory) where TStartupTask : class, IStartupTask
        {
            return services
                .AddScoped(factory)
                .AddScoped<IStartupTask, TStartupTask>(sp => sp.GetRequiredService<TStartupTask>());
        }

        public static IServiceCollection AddSharedStartupTask<TSharedStartupTask>(this IServiceCollection services) where TSharedStartupTask : class, ISharedStartupTask
        {
            return services
                .AddScoped<TSharedStartupTask>()
                .AddScoped<ISharedStartupTask, TSharedStartupTask>(sp => sp.GetRequiredService<TSharedStartupTask>());
        }

        public static IServiceCollection AddSharedStartupTask<TSharedStartupTask>(this IServiceCollection services, Func<IServiceProvider, TSharedStartupTask> factory) where TSharedStartupTask : class, ISharedStartupTask
        {
            return services
                .AddScoped(factory)
                .AddScoped<ISharedStartupTask, TSharedStartupTask>(sp => sp.GetRequiredService<TSharedStartupTask>());
        }
    }
}