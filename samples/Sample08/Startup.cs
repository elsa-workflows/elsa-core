using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.MassTransit.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Core.Extensions;
using Elsa.Core.Persistence.Extensions;
using Elsa.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Sample08.Messages;
using Sample08.Workflows;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Sample08
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
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Period.FromSeconds(10)))
                .AddEmailActivities(options => options.Bind(Configuration.GetSection("Smtp")))
                .AddRabbitMqActivities(
                    options => options.Bind(Configuration.GetSection("MassTransit:RabbitMq")), 
                    typeof(CreateOrder), 
                    typeof(OrderShipped))
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
            
            workflowRegistry.RegisterWorkflow<CreateOrderWorkflow>();
            workflowRegistry.RegisterWorkflow<HandleOrderWorkflow>();
        }
    }
}