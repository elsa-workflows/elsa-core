using Elsa.AspNetCore.Startup;
using Elsa.Runtime;
using Microsoft.AspNetCore.Hosting.Server;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a TaskExecutingServer that automatically invokes <see cref="IStartupRunner"/> when Kestrel starts.
        /// </summary>
        public static IServiceCollection AddTaskExecutingServer(this IServiceCollection services)
        {
            return services.Decorate<IServer, TaskExecutingServer>();
        }
    }
}