using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Activities.MassTransit.Consumers;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Multitenancy;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.MassTransitRabbitMq.Handlers;
using Elsa.Samples.MassTransitRabbitMq.Messages;
using Elsa.Samples.MassTransitRabbitMq.Workflows;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddElsaServices();

            // Register custom type definitions for intellisense.
            services.AddJavaScriptTypeDefinitionProvider<MessageTypeDefinitionProvider>();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            builder.ConfigureElsaServices(services, elsa => elsa
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                    .AddConsoleActivities()
                    .AddMassTransitActivities()
                    .AddWorkflow<TestWorkflow>()
                    .AddWorkflow<InterfaceTestWorkflow>());

            // Register notification handlers from this project. Will register ConfigureJavaScriptEngine.
            builder.AddNotificationHandlersFrom<Startup>();

            builder.Populate(services);
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

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateSampleMultitenantContainer(container);
        }
    }
}