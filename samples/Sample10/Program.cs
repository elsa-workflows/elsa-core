using System;
using System.Data;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Provider.Sqlite;

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

            using (var scope = services.CreateScope())
            {
                // Persist the workflow definition.
                var definitionStore = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
                await definitionStore.AddAsync(workflowDefinition);

                // Load the workflow definition.
                workflowDefinition = await definitionStore.GetByIdAsync(workflowDefinition.Id, VersionOptions.Latest);

                // Execute the workflow.
                var invoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
                var executionContext = await invoker.InvokeAsync(workflowDefinition);

                // Persist the workflow instance.
                var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                var workflowInstance = executionContext.Workflow.ToInstance();
                await instanceStore.SaveAsync(workflowInstance);
            }
        }
        
        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddWorkflowsCore()
                .AddStartupRunner()
                .AddConsoleActivities()
                .AddYesSql(options => options
                    .UseSqLite(@"Data Source=c:\data\elsa.yessql.db;Cache=Shared", IsolationLevel.ReadUncommitted)
                    .UseDefaultIdGenerator()
                    .SetTablePrefix("elsa_")
                )
                .AddYesSqlWorkflowDefinitionStore()
                .AddYesSqlWorkflowInstanceStore()
                .BuildServiceProvider();
        }
    }
}