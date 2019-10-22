using Elsa.Activities.Dropbox.Extensions;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.WorkflowHost.Web
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
                .AddWorkflows(
                    elsa => elsa.AddEntityFrameworkStores(
                        options => options
                            .UseSqlite(Configuration.GetConnectionString("Sqlite"))))
                .AddTaskExecutingServer()
                .AddHttpActivities(options => options.Bind(Configuration.GetSection("Http")))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddTimerActivities(options => options.Bind(Configuration.GetSection("BackgroundRunner")))
                .AddDropboxActivities(options => options.Bind(Configuration.GetSection("Dropbox")));
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseHttpActivities()
                .UseWelcomePage();
        }
    }
}