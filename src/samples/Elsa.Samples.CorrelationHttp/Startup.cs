using System.Data;
using Elsa.Samples.CorrelationHttp.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.CorrelationHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa()
                .AddHttpActivities()
                .AddConsoleActivities()
                .AddWorkflow<RegistrationWorkflow>();
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