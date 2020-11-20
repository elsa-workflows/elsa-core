using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Consumers;
using Elsa.Messages;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Rebus.DataBus.InMem;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Rebus.Transport.InMem;

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

            var transport = _serviceProvider.GetService<InMemNetwork>();
            var store = _serviceProvider.GetRequiredService<InMemorySubscriberStore>();
            var queueName = "run_Workflow";

            // Sender
            _serviceProvider
                .AddConsumer<RunWorkflow, RunWorkflowConsumer>("run_workflow:sender", (bus, _) => bus
                    .Logging(l => l.ColoredConsole())
                    .Transport(t => t.UseInMemoryTransportAsOneWayClient(transport)));
            
            // Receiver
            _serviceProvider
                .AddConsumer<RunWorkflow, RunWorkflowConsumer>("run_workflow:client", (bus, _) => bus
                    .Logging(l => l.ColoredConsole())
                    .Subscriptions(s => s.StoreInMemory(store))
                    .Transport(t => t.UseInMemoryTransport(transport, queueName))
                    .Routing(r => r.TypeBased().Map<RunWorkflow>(queueName)));

            return Task.CompletedTask;
        }
    }
}