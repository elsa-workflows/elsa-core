using Elsa.Samples.ContextualWorkflowHttp.Indexes;
using Elsa.Samples.ContextualWorkflowHttp.WorkflowContextProviders;
using Elsa.Samples.ContextualWorkflowHttp.Workflows;
using Elsa.Persistence.YesSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.Sqlite;
using System.Data;

namespace Elsa.Samples.ContextualWorkflowHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(options => options.UseYesSqlPersistence())
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