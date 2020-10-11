using Elsa.Activities.Console;
using Elsa.Models;
using Elsa.Services;
using Elsa.Builders;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Elsa.Samples.WorkflowDefinition
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var services = new ServiceCollection()
                               .AddElsa()
                               .AddLogging(log => log.AddConsole())
                               .AddConsoleActivities()
                               .AddSingleton(Console.In)
                               .BuildServiceProvider();

            var activityResolver = services.GetRequiredService<IActivityActivator>();
            var activity1 = activityResolver.ActivateActivity<WriteLine>()
                                            .WithId("activity-1")
                                            .WithText("Hello world!");

            var activity2 = activityResolver.ActivateActivity<WriteLine>()
                                            .WithId("activity-2")
                                            .WithText("Goodbye cruel world...!");

            // Create Workflow Definition.
            var workflowDefinition = new Models.WorkflowDefinition
            {
                WorkflowDefinitionVersionId = "definition-001",
                IsPublished = true,
                Activities = new List<ActivityDefinition>
                {
                    ActivityDefinition.FromActivity(activity1),
                    ActivityDefinition.FromActivity(activity2)
                },
                Connections = new[]
                {
                    new ConnectionDefinition("activity-1", "activity-2", OutcomeNames.Done),
                }
            };

            // Register it.
            //var workflowDefinitionStore = services.GetService<IWorkflowDefinitionStore>();
            //await workflowDefinitionStore.AddAsync(workflowDefinition);

            // Run it.
            var invoker = services.GetService<IWorkflowHost>();
            await invoker.RunWorkflowDefinitionAsync("definition-001");

            Console.ReadLine();
        }
    }
}