using System;
using Elsa.Activities.MassTransit.Extensions;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.QuartzIntegration;
using Quartz;
using Sample27.Consumers;
using Sample27.Messages;
using Elsa.Activities.MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MassTransit.Azure.ServiceBus.Core;

namespace Sample27
{
    internal class AzureServiceBusSchedulerMassTransitBuilder : MassTransitBuilderBase<AzureServiceBusSchedulerOptions>
    {
        private readonly IScheduler scheduler;

        public AzureServiceBusSchedulerMassTransitBuilder(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        protected override IBusControl CreateBus(IServiceProvider serviceProvider)
        {
            var bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusSchedulerOptions>>().Value;
                cfg.Host(options.ConnectionString);

                cfg.UseMessageScheduler(options.MessageSchedule.SchedulerAddress);

                cfg.ReceiveEndpoint("shopping_cart_service", ep =>
                {
                    ep.ConfigureConsumer<CartRemovedConsumer>(serviceProvider);
                });

                cfg.ReceiveEndpoint("shopping_cart_state", ep =>
                {
                    ep.PrefetchCount = 16;

                    ep.UseMessageRetry(r => r.Interval(2, 100));

                    // Consume all workflow messages from the same queue.
                    ep.ConfigureWorkflowConsumer<CartCreated>(serviceProvider);
                    ep.ConfigureWorkflowConsumer<CartItemAdded>(serviceProvider);
                    ep.ConfigureWorkflowConsumer<OrderSubmitted>(serviceProvider);
                    ep.ConfigureWorkflowConsumer<CartExpiredEvent>(serviceProvider);
                });

                // Should use external process scheduler service
                // https://github.com/MassTransit/MassTransit/tree/develop/src/Samples/MassTransit.QuartzService
                cfg.ReceiveEndpoint("sample_quartz_scheduler", e =>
                {
                    // For MT4.0, prefetch must be set for Quartz prior to anything else
                    e.PrefetchCount = 1;
                    cfg.UseMessageScheduler(e.InputAddress);

                    e.ConfigureConsumer<ScheduleMessageConsumer>(serviceProvider);
                    e.ConfigureConsumer<CancelScheduledMessageConsumer>(serviceProvider);
                });
            });

            scheduler.JobFactory = new MassTransitJobFactory(bus);

            return bus;
        }

        protected override void ConfigureMassTransit(IServiceCollectionConfigurator configurator)
        {
            // Configure scheduler service consumers.
            configurator.AddConsumer<ScheduleMessageConsumer>();
            configurator.AddConsumer<CancelScheduledMessageConsumer>();

            // configure workflow consumers
            configurator.AddWorkflowConsumer<CartCreated>();
            configurator.AddWorkflowConsumer<CartItemAdded>();
            configurator.AddWorkflowConsumer<OrderSubmitted>();
            configurator.AddWorkflowConsumer<CartExpiredEvent>();

            // host fake service consumers
            configurator.AddConsumer<CartRemovedConsumer>();
        }
    }
}