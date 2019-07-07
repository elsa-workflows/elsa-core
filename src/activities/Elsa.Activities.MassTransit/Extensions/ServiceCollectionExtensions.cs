using System;
using System.Collections.Generic;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Activities.MassTransit.Options;
using Elsa.Core.Extensions;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using MassTransit.AspNetCoreIntegration.HealthChecks;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.MassTransit.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqActivities(this IServiceCollection services, Action<OptionsBuilder<RabbitMqOptions>> options = null, params Type[] messageTypes)
        {
            var optionsBuilder = services.AddOptions<RabbitMqOptions>();
            options?.Invoke(optionsBuilder);
            
            services
                .AddActivity<SendMassTransitMessage>()
                .AddActivity<ReceiveMassTransitMessage>();

            services.AddSingleton<SimplifiedBusHealthCheck>();
            services.AddSingleton<ReceiveEndpointHealthCheck>();
            
            services.AddMassTransit(
                massTransit =>
                {
                    massTransit.AddBus(sp => CreateUsingRabbitMq(massTransit, sp, messageTypes));
                });

            services.AddSingleton<IHostedService, MassTransitHostedService>();

            foreach (var messageType in messageTypes)
            {
                var consumerType = CreateConsumerType(messageType);
                services.AddSingleton(consumerType);
            }

            return services;
        }

        private static IBusControl CreateUsingRabbitMq(IServiceCollectionConfigurator massTransit, IServiceProvider sp, IEnumerable<Type> messageTypes)
        {
            return Bus.Factory.CreateUsingRabbitMq(
                bus =>
                {
                    var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>();
                    var host = bus.Host(new Uri(options.Value.Host), _ => { });

                    foreach (var messageType in messageTypes)
                    {
                        var queueName = messageType.Name;
                        var consumerType = CreateConsumerType(messageType);

                        bus.ReceiveEndpoint(
                            host,
                            queueName,
                            endpoint =>
                            {
                                endpoint.PrefetchCount = 16;
                                endpoint.Consumer(consumerType, sp.GetRequiredService);
                                MapEndpointConvention(messageType, endpoint.InputAddress);
                            });
                    }
                });
        }

        private static void MapEndpointConvention(Type messageType, Uri destinationAddress)
        {
            var method = typeof(EndpointConvention).GetMethod("Map", new[]{ typeof(Uri) });
            var generic = method.MakeGenericMethod(messageType);
            generic.Invoke(null, new object[]{destinationAddress});
        }

        private static Type CreateConsumerType(Type messageType)
        {
            return typeof(WorkflowConsumer<>).MakeGenericType(messageType);
        }
    }
}