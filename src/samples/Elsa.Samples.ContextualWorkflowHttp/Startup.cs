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
                .AddElsa()
                .AddHttpActivities()
                .AddConsoleActivities()
                .AddDataMigration<Migrations>()
                .AddIndexProvider<DocumentIndexProvider>()
                .AddWorkflowContextProvider<DocumentWorkflowContextProvider>()
                .AddWorkflow<DocumentApprovalWorkflow>();
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