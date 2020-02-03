using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Dashboard.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;

namespace Elsa.Dashboard.Web
{
    public class Startup
    {
        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var elsaSection = Configuration.GetSection("Elsa");

            services
                .AddElsa(x => x.UseEntityFrameworkWorkflowStores(x => x.UseSqlite(Configuration.GetConnectionString("Sqlite"))));

            services
                .AddHttp(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmail(options => options.Bind(elsaSection.GetSection("Smtp")))
                .AddUserTaskActivities()
                .AddTimerActivities(options => options.Bind(elsaSection.GetSection("Timers")))
                .AddElsaDashboard();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                .UseWelcomePage();
        }
    }
}