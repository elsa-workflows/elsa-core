using Elsa.Persistence.InMemory;
using Elsa.Samples.CorrelationHttp.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.CorrelationHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa()
                .AddElsaPersistenceInMemory()
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