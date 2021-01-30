using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.FaultyWorkflows
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(options => options
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