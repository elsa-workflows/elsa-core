using System.Threading;
using Elsa.Activities.Cron.Extensions;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Primitives.Extensions;
using Elsa.Persistence;
using Elsa.Persistence.InMemory.Extensions;
using Elsa.Runtime.Extensions;
using Elsa.Runtime.Hosting.Extensions;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowHostProgram
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            // Needed for parsing NodaTime values from configuration.
            ProgramExtensions.ConfigureTypeConverters();
            
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddWorkflowsHost()
                .AddInMemoryWorkflowDefinitionStoreProvider()
                .AddInMemoryWorkflowInstanceStoreProvider()
                .AddPrimitiveActivities()
                .AddCronActivities(Configuration.GetSection("Workflows:Crontab"))
                .AddEmailActivities(Configuration.GetSection("Workflows:Smtp"))
                .AddHttpActivities();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowSerializer workflowSerializer)
        {
            // Add middleware that intercepts requests to be handled by workflows.
            app.UseHttpActivities();

            // Add a sample workflow definition.
            var sampleWorkflow = workflowSerializer.DeserializeAsync(WorkflowsResource.SampleWorkflowDefinition, YamlTokenFormatter.FormatName, CancellationToken.None).Result;
            workflowDefinitionStore.SaveAsync(sampleWorkflow, CancellationToken.None).Wait();
        }
    }
}