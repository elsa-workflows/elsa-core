using Elsa.Persistence.InMemory;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.HelloWorldHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa()
                .AddElsaPersistenceInMemory()
                .AddHttpActivities()
                .AddWorkflow<HelloHttpWorkflow>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add HTTP activities middleware.
            app.UseHttpActivities();
        }
    }
}