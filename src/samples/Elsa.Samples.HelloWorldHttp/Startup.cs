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
                .AddHttpActivities()
                .AddWorkflow<HelloHttpWorkflow>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpActivities();
        }
    }
}