using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.FaultyWorkflows.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.FaultyWorkflows
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(elsa => elsa
                    .UseEntityFrameworkPersistence(options => options.UseSqlite())
                    .AddConsoleActivities()
                    .AddHttpActivities()
                    .AddWorkflow<FaultyWorkflow>());
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();
        }
    }
}