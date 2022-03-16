using System;
using MassTransit;
using Elsa.Samples.MassTransitRabbitMq.Messages;
using Elsa.Samples.MassTransitRabbitMq.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.MassTransitRabbitMq.Handlers;

namespace Elsa.Samples.MassTransitRabbitMq
{
    public class Startup
    {
        private Type CreateWorkflowConsumer(Type messageType) => typeof(WorkflowConsumer<>).MakeGenericType(messageType);

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElsaApiEndpoints();

            // Allow arbitrary client browser apps to access the API for demo purposes only.
            // In a production environment, make sure to allow only origins you trust.
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));
            
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
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                    .AddConsoleActivities()
                    .AddMassTransitActivities()
                    .AddWorkflow<TestWorkflow>()
                    .AddWorkflow<InterfaceTestWorkflow>()
                );

            // Register notification handlers from this project. Will register ConfigureJavaScriptEngine.
            services.AddNotificationHandlersFrom<Startup>();

            // Register custom type definitions for intellisense.
            services.AddJavaScriptTypeDefinitionProvider<MessageTypeDefinitionProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}