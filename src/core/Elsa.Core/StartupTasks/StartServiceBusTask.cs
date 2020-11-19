using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Handlers;
using Rebus.ServiceProvider;

namespace Elsa.StartupTasks
{
    public class StartServiceBusTask : IStartupTask
    {
        private readonly IServiceProvider _serviceProvider;
        public StartServiceBusTask(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var consumers = _serviceProvider.GetServices<IHandleMessages>();
            var messageTypes = consumers.Select(consumer => consumer.GetType().GetInterfaces().First(x => x.GenericTypeArguments.Any()).GenericTypeArguments.First());

            _serviceProvider.UseRebus(async bus =>
            {
                foreach (var messageType in messageTypes)
                    await bus.Subscribe(messageType);
            });

            return Task.CompletedTask;
        }
    }
}