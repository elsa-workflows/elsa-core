using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Activities.RabbitMq.Decorators;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Routing.TransportMessages;
using Rebus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class Client : IClient
    {
        private BuiltinHandlerActivator _activator { get; set; }
        private IBus _bus { get; set; }

        public MessageHandlingToggle Toggle { get; set; }

        public RabbitMqBusConfiguration Configuration { get; }

        public Client(RabbitMqBusConfiguration configuration)
        {
            Configuration = configuration;
            Toggle = new MessageHandlingToggle();
        }

        public void StartWithHandler(Func<TransportMessage, CancellationToken, Task> handler)
        {
            _activator = new BuiltinHandlerActivator();

            _bus = Configure
                .With(_activator)
                .Routing(r => r.AddTransportMessageForwarder(async transportMessage =>
                {
                    if (!AllHeadersMatch(transportMessage.Headers)) return ForwardAction.None;

                    //avoid more than one message being handled by an activity during a single suspension cycle
                    Toggle.IsReceivingMessages = false;

                    await handler(transportMessage, CancellationToken.None);

                    return ForwardAction.Ignore();
                }))
                .Transport(t =>
                {
                    t.UseRabbitMq(Configuration.ConnectionString, $"Elsa_{Guid.NewGuid()}").InputQueueOptions(o => o.SetAutoDelete(autoDelete: true));
                    t.Decorate(d =>
                    {
                        var transport = d.Get<ITransport>();
                        return new MessageHandlingTransportDecorator(transport, Toggle);
                    });
                })
                .Start();

            _bus.Advanced.Topics.Subscribe(Configuration.RoutingKey);
        }

        public void StartAsOneWayClient()
        {
            _activator = new BuiltinHandlerActivator();

            _bus = Configure
                .With(_activator)
                .Transport(t => t.UseRabbitMqAsOneWayClient(Configuration.ConnectionString).InputQueueOptions(o => o.SetAutoDelete(autoDelete: true)))
                .Start();
        }

        public async Task PublishMessage(string message)
        {
            await _bus.Advanced.Topics.Publish(Configuration.RoutingKey, message, Configuration.Headers);
        }

        public void Dispose()
        {
            _activator.Dispose();
        }

        public void SetIsReceivingMessages(bool isReceivingMessages)
        {
            Toggle.IsReceivingMessages = isReceivingMessages;
        }

        private bool AllHeadersMatch(Dictionary<string, string> messageHeaders) => Configuration.Headers.All(x => messageHeaders.ContainsKey(x.Key) && messageHeaders[x.Key] == x.Value);
    }
}