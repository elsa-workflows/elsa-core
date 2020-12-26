using System;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Models;
using Elsa.Scripting.Liquid.Services;
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
                Id = "SampleWorkflow",
                DefinitionVersionId = "1",
                Version = 1,
                IsPublished = true,
                IsLatest = true,
                IsEnabled = true,
                PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
                Activities = new[]
                {
                    new ActivityDefinition
                    {
                        ActivityId = "activity-1",
                        Type = nameof(WriteLine),
                        Properties = new ActivityDefinitionProperties
                        {
                            [nameof(WriteLine.Text)] = new()
                            {
                                Syntax = LiquidExpressionHandler.SyntaxName,
                                Expression = "Hello World!",
                                Type = typeof(string)
                            }
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
            var workflowBlueprint = materializer.CreateWorkflowBlueprint(deserializedWorkflowDefinition);

            // Execute workflow.
            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunWorkflowAsync(workflowBlueprint);
        }
    }
}