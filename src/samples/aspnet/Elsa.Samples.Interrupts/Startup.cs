using Elsa.Samples.Interrupts.Activities;
using Elsa.Samples.Interrupts.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Interrupts
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddHttpActivities()
                    .AddQuartzTemporalActivities()
                    .AddActivity<Sleep>()
                    .StartWorkflow<InterruptableWorkflow>()
                );
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