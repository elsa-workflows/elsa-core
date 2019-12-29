using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
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
    /// Demonstrates constructing workflow blueprints from a workflow definition. This tests that a workflow JSON definition can be reconstructed and executed.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();

            var evaluator = services.GetRequiredService<IExpressionEvaluator>();
            var definition = new ProcessDefinitionVersion
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
                                    new SetVariable(evaluator)
                                    {
                                        VariableName = "X",
                                        Value = new JavaScriptExpression<int>("(41 + input())")
                                    }, 
                                    new WriteLine { Text = new LiquidExpression<string>("Current item: {{ Input }}.") },
                                    new WriteLine { Text = new LiquidExpression<string>("Variable: {{ Variables.X }}.") },
                                }
                            }
                        }),
                    
                    ActivityDefinition.FromActivity(new SetVariable(evaluator)
                    {
                        Id = "set-variable-1",
                        VariableName = "X",
                        Value = new LiteralExpression<int>("0")
                    }),
                    
                    ActivityDefinition.FromActivity(
                        new While
                        {
                            Id ="while-1",
                            Condition = new JavaScriptExpression<bool>("X < 5"),
                            Activity = new Sequence
                            {
                                Activities = new List<IActivity>
                                {
                                    new WriteLine
                                    {
                                        Text = new JavaScriptExpression<string>("`X = ${X}.`")
                                    },
                                    new SetVariable(evaluator)
                                    {
                                        VariableName = "X",
                                        Value = new JavaScriptExpression<int>("X + 1")
                                    }
                                }
                            }
                        })
                },
                Connections = new List<ConnectionDefinition>
                {
                    new ConnectionDefinition("for-each-1", "set-variable-1", OutcomeNames.Done),
                    new ConnectionDefinition("set-variable-1", "while-1", OutcomeNames.Done),
                }
            };

            var serializer = services.GetRequiredService<IWorkflowSerializer>();
            var json = serializer.Serialize(definition, JsonTokenFormatter.FormatName);
            var deserializedDefinition = serializer.Deserialize<ProcessDefinitionVersion>(json, JsonTokenFormatter.FormatName);

            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunAsync(deserializedDefinition);
        }
    }
}