using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Persistence;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Persistence.YesSql.Options;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample10
{
    // A simple demonstration of using YesSql persistence providers.
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var services = BuildServices();

            // Invoke startup tasks.
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();
            
            // Create a workflow definition.
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = registry.RegisterWorkflow<HelloWorldWorkflow>();
            
            // Persist the workflow definition.
            var definitionStore = services.GetRequiredService<IWorkflowDefinitionStore>();
            await definitionStore.AddAsync(workflowDefinition);
            
            // Load the workflow definition.
            workflowDefinition = await definitionStore.GetByIdAsync(workflowDefinition.Id);
            
            // Execute the workflow.
            var invoker = services.GetRequiredService<IWorkflowInvoker>();
            var executionContext = await invoker.InvokeAsync(workflowDefinition);
            
            // Persist the workflow instance.
            var instanceStore = services.GetRequiredService<IWorkflowInstanceStore>();
            var workflowInstance = executionContext.Workflow.ToInstance();
            await instanceStore.SaveAsync(workflowInstance);
        }
        
        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddWorkflowsCore()
                .AddStartupRunner()
                .AddConsoleActivities()
                .AddYesSql(options => options.Configure(x =>
                    {
                        x.TablePrefix = "elsa";
                        x.ConnectionString = @"Data Source=c:\data\elsa.db;Cache=Shared";
                        x.Provider = DatabaseProvider.SqLite;
                    }
                ))
                .AddYesSqlWorkflowDefinitionStore()
                .AddYesSqlWorkflowInstanceStore()
                .BuildServiceProvider();
        }
    }
}