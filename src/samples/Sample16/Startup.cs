using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Dashboard.Extensions;
using Elsa.Persistence.MongoDb.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sample16
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
            var elsaSection = Configuration.GetSection("Elsa");
            
            services
                // Add workflow services.
                .AddElsa(x => x.AddMongoDbStores(Configuration, "Sample16", "MongoDb"))
                
                // Add activities we'd like to use.
                .AddHttpActivities(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmailActivities(options => options.Bind(elsaSection.GetSection("Smtp")))
                .AddTimerActivities(options => options.Bind(elsaSection.GetSection("BackgroundRunner")))
                
                // Add Dashboard services.
                .AddElsaDashboard();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) 
                app.UseDeveloperExceptionPage();

            app
                .UseStaticFiles()
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                .UseWelcomePage();
        }
    }
}