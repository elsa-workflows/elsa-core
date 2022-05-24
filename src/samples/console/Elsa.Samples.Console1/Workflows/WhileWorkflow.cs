using Elsa.Activities;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Samples.Console1.Workflows;

public static class WhileWorkflow
{
    public static IActivity Create()
    {
        var counter = new Variable<int>();
        var localVariable = new Variable<string>("Foo");

        return new Sequence
        {
            Variables = { counter },
            Activities =
            {
                new WriteLine("Looping..."),
                new While
                {
                    Condition = new Input<bool>(context => counter.Get(context) < 3),
                    Body = new Sequence
                    {
                        Variables = { localVariable },
                        Activities =
                        {
                            new WriteLine(context => $"Iteration {counter.Get(context) + 1}"),
                            new WriteLine(context => $"Local variable: {localVariable.Get(context)}"),
                            new SetVariable<string>
                            {
                                Variable = localVariable,
                                Value = new Input<string>("Bar")
                            },
                            new WriteLine(context => $"Updated local variable: {localVariable.Get(context)}"),
                            new SetVariable<int>
                            {
                                Variable = counter,
                                Value = new Input<int>(context => counter.Get(context) + 1)
                            },
                            new Event("Foo")
                        }
                    }
                },
                new WriteLine("Done.")
            }
        };
    }
}