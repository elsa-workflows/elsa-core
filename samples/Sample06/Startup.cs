using Elsa.Activities.Http.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.Extensions;
using Elsa.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample06
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
                .AddWorkflows()
                .AddHttpActivities()
                .AddMemoryWorkflowDefinitionStore()
                .AddMemoryWorkflowInstanceStore();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IWorkflowRegistry workflowRegistry)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseHttpActivities();
            app.UseWelcomePage();
            
            workflowRegistry.RegisterWorkflow<HelloWorldWorkflow>();
        }
    }
}