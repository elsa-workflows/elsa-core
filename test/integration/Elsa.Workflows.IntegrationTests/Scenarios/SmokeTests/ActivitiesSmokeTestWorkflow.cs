using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SmokeTests;

/// <summary>
/// Comprehensive smoke test workflow that exercises basic control flow and data manipulation activities.
/// Tests: Start, Finish, End, Sequence, Break, Complete, If, Switch, While, For, ForEach,
/// WriteLine, SetName, SetVariable&lt;T&gt;, SetVariable (untyped), SetOutput
/// </summary>
public class ActivitiesSmokeTestWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        // Variables for testing
        var counter = new Variable<int>("Counter", 0);
        var name = new Variable<string>("Name", "Initial");
        var result = new Variable<string>("Result", "");
        var items = new Variable<List<string>>("Items", ["A", "B", "C"]);
        var currentItem = new Variable<string>("CurrentItem", "");
        var loopCounter = new Variable<int>("LoopCounter", 0);
        var untypedVar = new Variable<object?>("UntypedVar", null);
        var switchValue = new Variable<int>("SwitchValue", 2);

        workflow.WithVariables(counter, name, result, items, currentItem, loopCounter, untypedVar, switchValue);

        workflow.Root = new Sequence
        {
            Activities =
            {
                // Test Start activity
                new Start(),

                // Test SetName
                new SetName
                {
                    Value = new("SmokeTestWorkflow")
                },

                // Test WriteLine
                new WriteLine(context => "=== Smoke Test Started ==="),

                // Test SetVariable<T>
                new SetVariable<string>
                {
                    Variable = name,
                    Value = new("Updated Name")
                },
                new WriteLine(context => $"Name: {name.Get(context)}"),

                // Test SetVariable (untyped)
                new SetVariable
                {
                    Variable = untypedVar,
                    Value = new("Untyped value")
                },
                new WriteLine(context => $"Untyped: {untypedVar.Get(context)}"),

                // Test If activity (condition true)
                new If(() => true)
                {
                    Then = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("If branch: True path executed"),
                            new SetVariable<int> { Variable = counter, Value = new(10) }
                        }
                    },
                    Else = new WriteLine("If branch: False path (should not execute)")
                },

                // Test Switch activity
                new Switch
                {
                    Cases =
                    {
                        new(
                            "Case1",
                            context => ValueTask.FromResult(switchValue.Get(context) == 1),
                            new WriteLine("Switch: Case 1 (should not execute)")
                        ),
                        new(
                            "Case2",
                            context => ValueTask.FromResult(switchValue.Get(context) == 2),
                            new Sequence
                            {
                                Activities =
                                {
                                    new WriteLine("Switch: Case 2 executed"),
                                    new SetVariable<string> { Variable = result, Value = new("Switch-2") }
                                }
                            }
                        ),
                        new(
                            "Case3",
                            context => ValueTask.FromResult(switchValue.Get(context) == 3),
                            new WriteLine("Switch: Case 3 (should not execute)")
                        )
                    },
                    Default = new WriteLine("Switch: Default (should not execute)")
                },

                // Test For loop with Break
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("For loop: Starting"),
                        new For
                        {
                            Start = new(0),
                            End = new(100),
                            Step = new(1),
                            Body = new Sequence
                            {
                                Activities =
                                {
                                    new SetVariable<int> { Variable = loopCounter, Value = new(context => loopCounter.Get(context) + 1) },
                                    // Break after 3 iterations
                                    new If(context => loopCounter.Get(context) >= 3)
                                    {
                                        Then = new Break()
                                    }
                                }
                            }
                        },
                        new WriteLine(context => $"For loop: Completed with {loopCounter.Get(context)} iterations")
                    }
                },

                // Test While loop with Break
                new Sequence
                {
                    Activities =
                    {
                        new SetVariable<int> { Variable = loopCounter, Value = new(0) },
                        new WriteLine("While loop: Starting"),
                        new While(() => true)
                        {
                            Body = new Sequence
                            {
                                Activities =
                                {
                                    new SetVariable<int> { Variable = loopCounter, Value = new(context => loopCounter.Get(context) + 1) },
                                    new WriteLine(context => $"While loop: Iteration {loopCounter.Get(context)}"),
                                    // Break after 3 iterations
                                    new If(context => loopCounter.Get(context) >= 3)
                                    {
                                        Then = new Break()
                                    }
                                }
                            }
                        },
                        new WriteLine(context => $"While loop: Completed with {loopCounter.Get(context)} iterations")
                    }
                },

                // Test ForEach with Break
                new Sequence
                {
                    Activities =
                    {
                        new SetVariable<int> { Variable = loopCounter, Value = new(0) },
                        new WriteLine("ForEach loop: Starting"),
                        new ForEach<string>
                        {
                            Items = new(items),
                            CurrentValue = new(currentItem),
                            Body = new Sequence
                            {
                                Activities =
                                {
                                    new SetVariable<int> { Variable = loopCounter, Value = new(context => loopCounter.Get(context) + 1) },
                                    new WriteLine(context => $"ForEach: Item '{currentItem.Get(context)}'"),
                                    // Break after processing 2 items (A, B)
                                    new If(context => loopCounter.Get(context) >= 2)
                                    {
                                        Then = new Break()
                                    }
                                }
                            }
                        },
                        new WriteLine(context => $"ForEach loop: Completed with {loopCounter.Get(context)} items processed")
                    }
                },

                // Test SetOutput
                new SetOutput
                {
                    OutputName = new("FinalResult"),
                    OutputValue = new(context => $"Counter={counter.Get(context)}, Name={name.Get(context)}, Result={result.Get(context)}")
                },

                // Test Finish and End activities
                new Finish(),
                new End(),

                // Test Complete activity (ends workflow immediately)
                new Complete(),

                // This should not execute due to Complete
                new WriteLine("After Complete (should not execute)")
            }
        };
    }
}
