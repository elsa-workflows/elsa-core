using Elsa.Activities.RabbitMq.Configuration;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Routing.TransportMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class Client : IClient
    {
        private BuiltinHandlerActivator _activator;
        private IBus? _bus;

        public RabbitMqBusConfiguration Configuration { get; }

        public Client(RabbitMqBusConfiguration configuration)
        {
            Configuration = configuration;
            _activator = new BuiltinHandlerActivator();
        }

        public void SubscribeWithHandler(Func<TransportMessage, CancellationToken, Task> handler)
        {
            if (_bus != null) return;

            _bus = Configure
                .With(_activator)
                .Options(o => o.SetNumberOfWorkers(0))
                .Routing(r => r.AddTransportMessageForwarder(async transportMessage =>
                {
                    if (!AllHeadersMatch(transportMessage.Headers)) return ForwardAction.None;

                    await handler(transportMessage, CancellationToken.None);

                    return ForwardAction.Ignore();
                }))
                .Transport(t =>
                {
                    t.UseRabbitMq(Configuration.ConnectionString, $"Elsa{Guid.NewGuid().ToString("n").ToUpper()}");
                })
                .Start();

            _bus.Advanced.Topics.Subscribe(Configuration.TopicFullName);
        }

        public async Task PublishMessage(string message)
        {
            if (_bus == null) ConfigureAsOneWayClient();

            await _bus!.Advanced.Topics.Publish(Configuration.TopicFullName, message, Configuration.Headers);
        }

        public void Dispose()
        {
            _activator.Dispose();
        }

        public void StartClient()
        {
            if (_bus?.Advanced.Workers.Count == 0)
                _bus.Advanced.Workers.SetNumberOfWorkers(1);
        }

        public void StopClient()
        {
            if (_bus?.Advanced.Workers.Count == 1)
                _bus.Advanced.Workers.SetNumberOfWorkers(0);
        }

        private void ConfigureAsOneWayClient()
        {
            _bus = Configure
                .With(_activator)
                .Transport(t => t.UseRabbitMqAsOneWayClient(Configuration.ConnectionString).InputQueueOptions(o => o.SetAutoDelete(autoDelete: true)))
                .Start();
        }

        private bool AllHeadersMatch(Dictionary<string, string> messageHeaders) => Configuration.Headers.All(x => messageHeaders.ContainsKey(x.Key) && messageHeaders[x.Key] == x.Value);
    }
}