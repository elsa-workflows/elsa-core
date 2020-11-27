using Elsa.Samples.ReadModelHttp.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.ReadModelHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(option => option.UsePersistence(db => db.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                .AddHttpActivities()
                .AddConsoleActivities()
                .AddWorkflow<SaveContactWorkflow>();
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