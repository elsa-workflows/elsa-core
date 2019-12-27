using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Models;
using Elsa.Scripting.JavaScript;
using Elsa.Scripting.Liquid;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Sample26
{
    /// <summary>
    /// Demonstrates constructing workflow blueprints from a workflow definition.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();

            var definition = new WorkflowDefinitionVersion
            {
                IsPublished = true,
                Activities = new List<ActivityDefinition>
                {
                    ActivityDefinition.FromActivity(
                        new ForEach
                        {
                            Id = "for-each-1",
                            Collection = new JavaScriptExpression<IList<object>>("[1, 2, 3]"),
                            Activity = new Sequence
                            {
                                Activities = new IActivity[]
                                {
                                    new WriteLine { Text = new LiquidExpression<string>("Current item: {{ Input }}.") },
                                    new WriteLine { Text = new LiquidExpression<string>("....") },
                                }
                            }
                        })
                }
            };

            var serializer = services.GetRequiredService<IWorkflowSerializer>();
            var json = serializer.Serialize(definition, JsonTokenFormatter.FormatName);
            var deserializedDefinition = serializer.Deserialize<WorkflowDefinitionVersion>(json, JsonTokenFormatter.FormatName);

            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunAsync(deserializedDefinition);
        }
    }
}