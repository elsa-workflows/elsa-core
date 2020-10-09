using Elsa.Persistence.Core.Extensions;
using Elsa.Samples.TimesheetApproval.Models;
using Elsa.Samples.TimesheetApproval.Services;
using Elsa.Samples.TimesheetApproval.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.TimesheetApproval
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
            // Read configuration.
            var connectionString = Configuration.GetConnectionString("MongoDb");
            var elsaSection = Configuration.GetSection("Elsa");

            // Add API controller services.
            services.AddControllers();
            
            // Add custom application services.
            services
                .AddScoped<TimesheetManager>();

            // Add Elsa services
            services.AddElsa(elsa => elsa.UsePersistence(_ => { }));
            
            // Add activities and their services.
            services
                .AddHttpActivities(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmailActivities(options => options.Bind(elsaSection.GetSection("Smtp")))
                .AddTimerActivities(options => options.Bind(elsaSection.GetSection("BackgroundRunner")))
                .AddActivity<Activities.TimesheetSubmitted>()
                .AddActivity<Activities.TimesheetApproved>()
                .AddActivity<Activities.TimesheetRejected>();
                
            // Add workflows.
            services.AddWorkflow<TimesheetApprovalWorkflow>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                .UseHttpActivities()
                .UseWelcomePage();
        }
    }
}