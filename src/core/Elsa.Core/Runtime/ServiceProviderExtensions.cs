using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.ServiceProvider;

namespace Elsa.Runtime
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Starts the service bus and registers workflow message consumers. 
        /// </summary>
        public static async Task<IServiceProvider> StartElsaAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var bus = serviceProvider.GetRequiredService<IBus>();
            
            serviceProvider.UseRebus();
            
            await bus.Subscribe<RunWorkflow>();

            return serviceProvider;
        }
    }
}