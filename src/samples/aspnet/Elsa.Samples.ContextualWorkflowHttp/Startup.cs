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
            services
                .AddElsa(options => options
                    .UseYesSqlPersistence()
                    .AddHttpActivities()
                    .AddConsoleActivities()
                    .AddWorkflow<DocumentApprovalWorkflow>())
                .AddDataMigration<Migrations>()
                .AddIndexProvider<DocumentIndexProvider>()
                .AddWorkflowContextProvider<DocumentWorkflowContextProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();

            // Show welcome page if no routes matched any endpoints.
            app.UseWelcomePage();
        }
    }
}