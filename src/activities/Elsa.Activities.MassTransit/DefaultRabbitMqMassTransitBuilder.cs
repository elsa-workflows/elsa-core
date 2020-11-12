using System;
using System.Collections.Generic;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.MassTransit.Options;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.MassTransit
{
    public sealed class DefaultRabbitMqMassTransitBuilder : MassTransitBuilderBase<RabbitMqOptions>
    {
        protected override IBusControl CreateBus(IServiceProvider serviceProvider)
        {
            return Bus.Factory.CreateUsingRabbitMq(bus =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var host = bus.Host(new Uri(options.Host), h =>
                {
                    if (!string.IsNullOrEmpty(options.Username))
                    {
                        h.Username(options.Username);

                        if (!string.IsNullOrEmpty(options.Password))
                        {
                            h.Password(options.Password);
                        }
                    }
                });

                foreach (var messageType in MessageTypes)
                {
                    var queueName = messageType.Name;
                    var consumerType = messageType.CreateConsumerType();

                    bus.ReceiveEndpoint(queueName, endpoint =>
                    {
                        endpoint.PrefetchCount = 16;
                        endpoint.ConfigureConsumer(serviceProvider, consumerType);
                        messageType.MapEndpointConvention(endpoint.InputAddress);
                    });
                }
            });
        }

        protected override void ConfigureMassTransit(IServiceCollectionConfigurator configurator)
        {
            foreach (var messageType in MessageTypes)
            {
                configurator.AddConsumer(messageType.CreateConsumerType());
            }
        }

        public IEnumerable<Type> MessageTypes { get; set; }
    }
}