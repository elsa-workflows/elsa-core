using System;
using MassTransit;
using Elsa.Samples.MassTransitRabbitMq.Messages;
using Elsa.Samples.MassTransitRabbitMq.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.MassTransit.Consumers;

namespace Elsa.Samples.MassTransitRabbitMq
{
    public class Startup
    {
        private Type CreateWorkflowConsumer(Type messageType) => typeof(WorkflowConsumer<>).MakeGenericType(messageType);

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services
                .AddMassTransit(x =>
                {
                    // Add workflow consumer for message
                    x.AddConsumer(CreateWorkflowConsumer(typeof(FirstMessage)));
                    x.AddConsumer(CreateWorkflowConsumer(typeof(SecondMessage)));
                    x.AddConsumer(CreateWorkflowConsumer(typeof(IInterfaceMessage)));

                    // Configure rabbitmq
                    x.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.ConfigureEndpoints(ctx);
                        cfg.Host("rabbitmq://guest:guest@localhost");
                    });
                })
                .AddMassTransitHostedService();

            services
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddMassTransitActivities()
                    .AddWorkflow<TestWorkflow>()
                    .AddWorkflow<InterfaceTestWorkflow>()
                );
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}