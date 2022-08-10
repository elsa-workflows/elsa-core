using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Multitenancy;
using Elsa.Persistence.YesSql;
using Elsa.Samples.ContextualWorkflowHttp.Indexes;
using Elsa.Samples.ContextualWorkflowHttp.WorkflowContextProviders;
using Elsa.Samples.ContextualWorkflowHttp.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.ContextualWorkflowHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddElsaServices();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.

            var services = new ServiceCollection();

            builder.ConfigureElsaServices(services, elsa => elsa
                    .AddElsaMultitenancy()
                    .UseYesSqlPersistence()
                    .AddHttpActivities()
                    .AddConsoleActivities()
                    .AddWorkflow<DocumentApprovalWorkflow>())
                    .AddDataMigration<Migrations>()
                    .AddIndexProvider<DocumentIndexProvider>()
                    .AddWorkflowContextProvider<DocumentWorkflowContextProvider>();

            builder.Populate(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();

            // Show welcome page if no routes matched any endpoints.
            app.UseWelcomePage();
        }

        public static MultitenantContainer ConfigureMultitenantContainer(IContainer container)
        {
            return MultitenantContainerFactory.CreateSampleMultitenantContainer(container);
        }
    }
}