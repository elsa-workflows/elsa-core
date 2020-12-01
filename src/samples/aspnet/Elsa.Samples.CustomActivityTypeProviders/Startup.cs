using Elsa.Samples.CustomActivityTypeProviders.Activities;
using Elsa.Samples.CustomActivityTypeProviders.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.CustomActivityTypeProviders
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services
                .AddElsa()
                .AddConsoleActivities()
                .AddHttpActivities()
                .AddTimerActivities()
                .AddActivity<Sleep>()
                .StartWorkflow<InterruptableWorkflow>();
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpActivities();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseWelcomePage();
        }
    }
}