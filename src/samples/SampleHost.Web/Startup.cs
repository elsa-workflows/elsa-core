using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Cron.Extensions;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Primitives.Extensions;
using Elsa.Persistence.FileSystem.Extensions;
using Elsa.Runtime.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleHost.Web
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
                .AddLocalization()
                .AddWorkflowsHost()
                .AddFileSystemWorkflowDefinitionStoreProvider(Configuration.GetSection("FileStore"))
                .AddFileSystemWorkflowInstanceStoreProvider(Configuration.GetSection("FileStore"))
                .AddPrimitiveActivities()
                .AddConsoleActivities()
                .AddHttpActivities()
                .AddEmailActivities(Configuration.GetSection("Smtp"))
                .AddCronActivities(Configuration.GetSection("Crontab"));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Add middleware that intercepts requests to be handled by workflows.
            app.UseHttpActivities();
        }
    }
}