using System;
using System.Collections.Generic;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Activities.MassTransit.Options;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using MassTransit.ConsumeConfigurators;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.MassTransit.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMassTransitActivities(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services
                .AddActivity<PublishMassTransitMessage>()
                .AddActivity<ReceiveMassTransitMessage>()
                .AddActivity<SendMassTransitMessage>();
        }

        public static IServiceCollection AddMassTransitSchedulingActivities(this IServiceCollection services, Action<MessageScheduleOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddMassTransitActivities()
                .AddActivity<CancelScheduledMassTransitMessage>()
                .AddActivity<ScheduleSendMassTransitMessage>();

            services.Configure(configureOptions);
            return services;
        }

        public static IServiceCollection AddRabbitMqActivities(this IServiceCollection services, Action<OptionsBuilder<RabbitMqOptions>> options = null, params Type[] messageTypes)
        {
            var optionsBuilder = services.AddOptions<RabbitMqOptions>();
            options?.Invoke(optionsBuilder);

            services
                .AddMassTransitActivities()
                .AddMassTransit(CreateBus, ConfigureMassTransit);

            return services;

            // local function to configure consumers
            void ConfigureMassTransit(IServiceCollectionConfigurator configurator)
            {
                foreach (var messageType in messageTypes)
                {
                    configurator.AddConsumer(CreateConsumerType(messageType));
                }
            }

            // local function to create the bus
            IBusControl CreateBus(IServiceProvider sp)
            {
                return CreateUsingRabbitMq(sp, messageTypes);
            }
        }

        public static void ConfigureWorkflowConsumer<TMessage>(
            this IReceiveEndpointConfigurator configurator,
            IServiceProvider provider,
            Action<IConsumerConfigurator<WorkflowConsumer<TMessage>>> configure = null)
            where TMessage : class
        {
            provider.GetRequiredService<IRegistration>().ConfigureConsumer(configurator, configure);

            EndpointConvention.Map<TMessage>(configurator.InputAddress);
        }

        public static IConsumerRegistrationConfigurator<WorkflowConsumer<TMessage>> AddWorkflowConsumer<TMessage>(
            this IRegistrationConfigurator configurator,
            Action<IConsumerConfigurator<WorkflowConsumer<TMessage>>> configure = null)
            where TMessage : class
        {
            return configurator.AddConsumer(configure);
        }

        private static IBusControl CreateUsingRabbitMq(IServiceProvider sp, IEnumerable<Type> messageTypes)
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