using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.LoopingWorkflows.Workflows;

public static class ForWorkflow
{
    public static IActivity Create()
    {
        var currentValueVariable = new Variable<int>();

        return new Sequence
        {
            Variables = { currentValueVariable },
            Activities =
            {
                new WriteLine("Counting down from 10 to 1:"),
                new For(10, 1)
                {
                    CurrentValue = new Output<object?>(currentValueVariable),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Current value: {currentValueVariable.Get(context)}"),
                            new Delay(TimeSpan.FromSeconds(1))
                        }
                    }
                },
                new WriteLine("Happy coding!")
            }
        };
    }
}