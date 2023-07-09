using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Samples.ConsoleApp.LoopingWorkflows.Workflows;

public static class WhileWorkflow
{
    public static IActivity Create()
    {
        var counterVariable = new Variable<int>(1);

        return new Sequence
        {
            Variables = { counterVariable },
            Activities =
            {
                new WriteLine("Counting to 100:"),
                
                // Loop while counter < 100.
                new While(context => counterVariable.Get(context) <= 100)
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Current value: {counterVariable.Get(context)}"),
                            
                            // Increment counter variable.
                            new SetVariable<int>(counterVariable, context => counterVariable.Get(context) + 1)
                        }
                    }
                },
                new WriteLine("That was really fast!")
            }
        };
    }
}