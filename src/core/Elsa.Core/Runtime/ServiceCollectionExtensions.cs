using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStartupRunner(this IServiceCollection services)
        {
            services.TryAddTransient<IStartupRunner, StartupRunner>();
            return services;
        }

        public static IServiceCollection AddStartupTask<TStartupTask>(this IServiceCollection services)
            where TStartupTask : class, IStartupTask
        {
            return services
                .AddTransient<IStartupTask, TStartupTask>();
        }

        /// <summary>
        /// Registers a TaskExecutingServer that automatically invokes <see cref="IStartupRunner"/> when Kestrel starts.
        /// </summary>
        public static IServiceCollection AddTaskExecutingServer(this IServiceCollection services)
        {
            return services
                .AddStartupRunner()
                .Decorate<IServer, TaskExecutingServer>();
        }
    }
}