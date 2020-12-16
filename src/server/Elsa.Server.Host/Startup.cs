using Elsa.Runtime;
using Elsa.Server.Host.Workflows;
using Elsa.StartupTasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elsa.Persistence.YesSql;
using YesSql.Provider.Sqlite;
using System.Data;
using Elsa.Persistence.YesSql.Extensions;

namespace Elsa.Server.Host
{
    public class Startup
    {
        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        private IWebHostEnvironment Environment { get; }
        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var elsaSection = Configuration.GetSection("Elsa");

            services
                .AddElsa(options => options.UseYesSqlPersistence());

            services
                .AddElsaApiEndpoints()
                .AddElsaSwagger()
                .AddConsoleActivities()
                .AddHttpActivities(elsaSection.GetSection("Http").Bind)
                .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                .AddTimerActivities(elsaSection.GetSection("BackgroundRunner").Bind)
                .AddStartupTask<ResumeRunningWorkflowsTask>()
                .AddWorkflow(typeof(Startup).Assembly);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Elsa"));
            }

            app
                .UseCors()
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(configure => configure.MapControllers());
        }
    }
}