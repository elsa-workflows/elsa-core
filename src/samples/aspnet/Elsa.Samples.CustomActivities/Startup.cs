using Elsa.Persistence.InMemory;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.CustomActivities
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa()
                .AddElsaPersistenceInMemory()
                .AddHttpActivities()
                .AddActivity<ReadQueryString>()
                .AddWorkflow<EchoQueryStringWorkflow>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpActivities();
        }
    }
}