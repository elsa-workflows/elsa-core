using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.Extensions;
using Elsa.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sample07
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
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp"))) //Tip: use Smtp4Dev for a local SMTP server that displays sent emails without actually sending them.
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
            
            workflowRegistry.RegisterWorkflow<DocumentApprovalWorkflow>();
        }
    }
}