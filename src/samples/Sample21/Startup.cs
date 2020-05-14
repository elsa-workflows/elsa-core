using System;
using System.Collections.Specialized;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.Timers.Extensions;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.AspNetCoreIntegration;
using MassTransit.QuartzIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Sample21.Consumers;
using Sample21.Controllers;
using Sample21.Messages;
using Sample21.Services;
using Sample21.Workflows;

namespace Sample21
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers();

            var scheduler = CreateScheduler();

            services
                .AddElsa()
                .AddHttpActivities()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Duration.FromSeconds(10)))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddMassTransitSchedulingActivities(options =>
                {
                    options.SchedulerAddress = new Uri("rabbitmq://localhost/sample_quartz_scheduler");
                })
                .AddWorkflow<CartTrackingWorkflow>()

                .AddScoped<ICarts, Carts>()

                .AddSingleton(scheduler)

                // configures MassTransit to integrate with the built-in dependency injection
                .AddMassTransit(CreateBus, ConfigureMassTransit)

                // Add a hosted service to stat and stop the quartz scheduler
                .AddSingleton<IHostedService, QuartzHostedService>();

            void ConfigureMassTransit(IServiceCollectionConfigurator configurator)
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

            // local function to create the bus
            IBusControl CreateBus(IServiceProvider serviceProvider)
            {
                var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var host = cfg.Host(new Uri("rabbitmq://localhost"), h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.UseMessageScheduler(new Uri("rabbitmq://localhost/sample_quartz_scheduler"));

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

            IScheduler CreateScheduler()
            {
                LogProvider.SetCurrentLogProvider(new QuartzConsoleLogProvider());

                ISchedulerFactory schedulerFactory = new StdSchedulerFactory(new NameValueCollection()
                {
                    {"quartz.scheduler.instanceName", "Sample-QuartzScheduler"},
                    {"quartz.scheduler.instanceId", "AUTO"},
                    {"quartz.threadPool.type", "Quartz.Simpl.SimpleThreadPool, Quartz"},
                    {"quartz.threadPool.threadCount", "4"},
                    {"quartz.jobStore.misfireThreshold", "60000"},
                    {"quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz"},
                });

                var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

                return scheduler;
            }
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseStaticFiles()
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                .UseWelcomePage();
        }

        private class QuartzConsoleLogProvider : ILogProvider
        {
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    if (level >= Quartz.Logging.LogLevel.Debug && func != null)
                    {
                        Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                    }
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, string value)
            {
                throw new NotImplementedException();
            }
        }
    }
}