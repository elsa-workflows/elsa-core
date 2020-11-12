using System;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Activities.MassTransit.Options;
using MassTransit;
using MassTransit.ConsumeConfigurators;
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

        public static IServiceCollection AddMassTransitSchedulingActivities<TOptions>(this IServiceCollection services, 
            IMassTransitBuilder<TOptions> massTransitConfigurator,
            Action<OptionsBuilder<MessageScheduleOptions>> options) where TOptions : class
        {
            var optionsBuilder = services.AddOptions<MessageScheduleOptions>();
            options?.Invoke(optionsBuilder);

            services.AddMassTransitActivities()
                .AddActivity<CancelScheduledMassTransitMessage>()
                .AddActivity<ScheduleSendMassTransitMessage>();

            massTransitConfigurator.Build(services);
            return services;
        }

        public static IServiceCollection AddMassTransitActivities<TOptions>(this IServiceCollection services, 
            IMassTransitBuilder<TOptions> massTransitConfigurator) where TOptions : class
        {
            services.AddMassTransitActivities();
            massTransitConfigurator.Build(services);
            return services;
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
    }
}