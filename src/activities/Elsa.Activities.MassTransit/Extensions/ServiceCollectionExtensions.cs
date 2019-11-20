using System;
using System.Collections.Generic;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Activities.MassTransit.Options;
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
                .AddActivity<PublishMassTransitMessage>()
                .AddActivity<ReceiveMassTransitMessage>();

            services.AddSingleton<SimplifiedBusHealthCheck>();
            services.AddSingleton<ReceiveEndpointHealthCheck>();

            services.AddMassTransit(
                massTransit =>
                {
                    foreach (var messageType in messageTypes)
                    {
                        massTransit.AddConsumer(CreateConsumerType(messageType));
                    }

                    massTransit.AddBus(sp => CreateUsingRabbitMq(massTransit, sp, messageTypes));
                });

            services.AddSingleton<IHostedService, MassTransitHostedService>();

            return services;
        }

        private static IBusControl CreateUsingRabbitMq(IServiceCollectionConfigurator massTransit, IServiceProvider sp, IEnumerable<Type> messageTypes)
        {

            return Bus.Factory.CreateUsingRabbitMq(
                bus =>
                {
                    var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                    var host = bus.Host(new Uri(options.Host), h =>
                    {
                        if (!string.IsNullOrEmpty(options.Username))
                        {
                            h.Username(options.Username);

                            if (!string.IsNullOrEmpty(options.Password))
                                h.Password(options.Password);
                        }
                    });


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
                                endpoint.ConfigureConsumer(sp, consumerType);
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