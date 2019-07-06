using System;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Core.Extensions;
using Elsa.Core.Persistence.Extensions;
using Elsa.Services;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using MassTransit.RabbitMqTransport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Sample08.Activities;
using Sample08.Consumers;
using Sample08.Messages;
using Sample08.Workflows;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Sample08
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWorkflows()
                .AddHttpActivities()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Period.FromSeconds(5)))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddMassTransitActivities()
                .AddMemoryWorkflowDefinitionStore()
                .AddMemoryWorkflowInstanceStore();

            services
                .AddActivity<SendMassTransitMessage<CreateOrder>>()
                .AddActivity<ReceiveMassTransitMessage<CreateOrder>>()
                .AddActivity<ReceiveMassTransitMessage<OrderShipped>>();
            
            services.AddMassTransit(
                massTransit =>
                {
                    massTransit.AddConsumer<CreateOrderConsumer>();
                    massTransit.AddConsumer<OrderShippedConsumer>();
                    
                    massTransit.AddBus(sp => Bus.Factory.CreateUsingRabbitMq(
                        bus =>
                        {
                            var host = bus.Host(new Uri("rabbitmq://localhost:5672"), _ => { });

                            bus.ReceiveEndpoint(
                                host,
                                "create_order",
                                endpoint =>
                                {
                                    endpoint.PrefetchCount = 16;
                                    endpoint.Consumer(typeof(CreateOrderConsumer), sp.GetRequiredService);
                                });
                            
                            bus.ReceiveEndpoint(
                                host,
                                "order_shipped",
                                endpoint =>
                                {
                                    endpoint.PrefetchCount = 16;
                                    endpoint.Consumer(typeof(OrderShippedConsumer), sp.GetRequiredService);
                                });
                        }));
                });

            services.AddSingleton<IHostedService, MassTransitHostedService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IWorkflowRegistry workflowRegistry)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseHttpActivities();
            app.UseWelcomePage();
            
            workflowRegistry.RegisterWorkflow<CreateOrderWorkflow>();
        }
    }
}