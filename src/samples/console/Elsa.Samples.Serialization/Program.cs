using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Serialization
{
    /// <summary>
    /// Demonstrates how to:
    /// 1. Serialize a workflow definition model to JSON.
    /// 2. Deserialize JSON back to a workflow definition model.
    /// 3. Materialize a workflow definition model into a workflow blueprint.
    /// 4. Run a workflow blueprint.
    /// </summary>
    internal class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities())
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Define a workflow.
            var workflowDefinition = new WorkflowDefinition
            {
                Id = "1",
                DefinitionId = "SampleWorkflow",
                Version = 1,
                IsPublished = true,
                IsLatest = true,
                PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
                Activities = new[]
                {
                    new ActivityDefinition
                    {
                        ActivityId = "activity-1",
                        Type = nameof(WriteLine),
                        Properties = new List<ActivityDefinitionProperty>()
                        {
                            ActivityDefinitionProperty.Liquid(nameof(WriteLine.Text), "Hello World")
                        }
                    },
                }
            };

            // Serialize workflow definition to JSON.
            var serializer = services.GetRequiredService<IContentSerializer>();
            var json = serializer.Serialize(workflowDefinition);

            Console.WriteLine(json);

            // Deserialize workflow definition from JSON.
            var deserializedWorkflowDefinition = serializer.Deserialize<WorkflowDefinition>(json);

            // Materialize workflow.
            var materializer = services.GetRequiredService<IWorkflowBlueprintMaterializer>();
            var workflowBlueprint = await materializer.CreateWorkflowBlueprintAsync(deserializedWorkflowDefinition);

            // Execute workflow.
            var workflowRunner = services.GetRequiredService<IStartsWorkflow>();
            await workflowRunner.StartWorkflowAsync(workflowBlueprint);
        }
    }
}