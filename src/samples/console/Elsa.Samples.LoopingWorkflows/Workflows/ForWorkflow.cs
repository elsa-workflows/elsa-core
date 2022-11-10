using Elsa.Expressions.Models;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

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
                    CurrentValue = new Output<MemoryBlockReference?>(currentValueVariable),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Current value: {currentValueVariable.Get(context)}"),
                            new Delay
                            {
                                // The strategy determines whether the workflow should suspend or wait synchronously.
                                // For console applications without a host, long-running workflows aren't supported, so we use the Blocking strategy. 
                                Strategy = new Input<DelayBlockingStrategy>(DelayBlockingStrategy.Blocking),
                                TimeSpan = new Input<TimeSpan>(TimeSpan.FromSeconds(1))
                            }
                        }
                    }
                },
                new WriteLine("Happy coding!")
            }
        };
    }
}