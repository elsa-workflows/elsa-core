using System;
using System.Collections.Generic;
using Elsa.Activities.MassTransit.Bookmarks;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Activities.MassTransit.Options;
using Elsa.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.MassTransit.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddMassTransitActivities(this ElsaOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Services.AddBookmarkProvider<MessageReceivedTriggerProvider>();

            return options
                .AddActivity<PublishMassTransitMessage>()
                .AddActivity<ReceiveMassTransitMessage>()
                .AddActivity<SendMassTransitMessage>();
        }

        public static ElsaOptionsBuilder AddMassTransitSchedulingActivities(this ElsaOptionsBuilder options, Action<MessageScheduleOptions>? configureOptions)
        {
            options.AddMassTransitActivities()
                .AddActivity<CancelScheduledMassTransitMessage>()
                .AddActivity<ScheduleSendMassTransitMessage>();

            if(configureOptions != null)
                options.Services.Configure(configureOptions);
            
            return options;
        }

        public static ElsaOptionsBuilder AddRabbitMqActivities(this ElsaOptionsBuilder options, Action<RabbitMqOptions>? configureOptions = null, params Type[] messageTypes)
        {
            if (configureOptions != null) 
                options.Services.Configure(configureOptions);

            options
                .AddMassTransitActivities();
                //.AddMassTransit(CreateBus, ConfigureMassTransit);

            return options;

            // // Local function to configure consumers.
            // void ConfigureMassTransit(IServiceCollectionConfigurator configurator)
            // {
            //     foreach (var messageType in messageTypes)
            //     {
            //         configurator.AddConsumer(CreateConsumerType(messageType));
            //     }
            // }

            // Local function to create the bus.
            IBusControl CreateBus(IServiceProvider sp) => CreateUsingRabbitMq(sp, messageTypes);
        }

        public static void ConfigureWorkflowConsumer<TMessage>(
            this IReceiveEndpointConfigurator configurator,
            IRegistrationContext context,
            Action<IConsumerConfigurator<WorkflowConsumer<TMessage>>>? configure = null)
            where TMessage : class
        {
            configurator.ConfigureConsumer(context, configure);
            EndpointConvention.Map<TMessage>(configurator.InputAddress);
        }

        public static IConsumerRegistrationConfigurator<WorkflowConsumer<TMessage>> AddWorkflowConsumer<TMessage>(
            this IRegistrationConfigurator configurator,
            Action<IConsumerConfigurator<WorkflowConsumer<TMessage>>>? configure = null)
            where TMessage : class
        {
            return configurator.AddConsumer(configure);
        }

        private static IBusControl CreateUsingRabbitMq(IServiceProvider sp, IEnumerable<Type> messageTypes)
        {
            return Bus.Factory.CreateUsingRabbitMq(
                bus =>
                {
                    // var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                    // var host = bus.Host(new Uri(options.Host), h =>
                    // {
                    //     if (!string.IsNullOrEmpty(options.Username))
                    //     {
                    //         h.Username(options.Username);
                    //
                    //         if (!string.IsNullOrEmpty(options.Password))
                    //             h.Password(options.Password);
                    //     }
                    // });


                    foreach (var messageType in messageTypes)
                    {
                        var queueName = messageType.Name;
                        var consumerType = CreateConsumerType(messageType);

                        // bus.ReceiveEndpoint(
                        //     queueName,
                        //     endpoint =>
                        //     {
                        //         endpoint.PrefetchCount = 16;
                        //         endpoint.ConfigureConsumer(sp, consumerType);
                        //         MapEndpointConvention(messageType, endpoint.InputAddress);
                        //     });
                    }
                });
        }

        private static void MapEndpointConvention(Type messageType, Uri destinationAddress)
        {
            var method = typeof(EndpointConvention).GetMethod("Map", new[]{ typeof(Uri) });
            var generic = method.MakeGenericMethod(messageType);
            generic.Invoke(null, new object[]{destinationAddress});
        }

        private static Type CreateConsumerType(Type messageType) => typeof(WorkflowConsumer<>).MakeGenericType(messageType);
    }
}