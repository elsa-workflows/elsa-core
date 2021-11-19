using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.UniqueCorrelatedWorkflows
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers();
            
            services
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddWorkflowsFrom<Startup>()
                )
                .AddElsaApiEndpoints();
            
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseWelcomePage();
        }
    }
}