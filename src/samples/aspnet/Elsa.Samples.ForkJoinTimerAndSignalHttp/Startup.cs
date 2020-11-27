using Elsa.Samples.ForkJoinTimerAndSignalHttp.BackgroundTasks;
using Elsa.Samples.ForkJoinTimerAndSignalHttp.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.ForkJoinTimerAndSignalHttp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers();
            
            services
                .AddElsa()
                .AddConsoleActivities()
                .AddTimerActivities()
                .AddHostedService<WorkflowStarter<DemoWorkflow>>()
                .AddWorkflow<DemoWorkflow>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseWelcomePage();
        }
    }
}