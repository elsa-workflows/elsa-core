using Elsa.Samples.ForkJoinTimerAndSignalWorker.BackgroundTasks;
using Elsa.Samples.ForkJoinTimerAndSignalWorker.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Elsa.Samples.ForkJoinTimerAndSignalWorker
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
                .AddTimerActivities(options => options.SweepInterval = Duration.FromSeconds(1))
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