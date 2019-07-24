using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStartupTask<TStartupTask>(this IServiceCollection services)
            where TStartupTask : class, IStartupTask
            => services
                .AddTransient<IStartupTask, TStartupTask>()
                .AddTaskExecutingServer();

        private static IServiceCollection AddTaskExecutingServer(this IServiceCollection services)
        {
            var decoratorType = typeof(TaskExecutingServer);

            return services.Any(service => service.ImplementationType == decoratorType)
                ? services
                : services.Decorate<IServer, TaskExecutingServer>();
        }
    }
}