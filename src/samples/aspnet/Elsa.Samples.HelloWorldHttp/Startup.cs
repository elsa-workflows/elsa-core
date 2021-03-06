using ElsaDashboard.Backend.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.HelloWorldHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(options => options
                    .AddHttpActivities()
                    .AddWorkflow<HelloHttpWorkflow>());

            services.AddElsaDashboardUI();
            services.AddElsaDashboardBackend();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();
        }
    }
}