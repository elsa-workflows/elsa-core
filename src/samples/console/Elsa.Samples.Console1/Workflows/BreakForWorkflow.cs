using System.Linq.Expressions;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Console1.Workflows;

public static class BreakForWorkflow
{
    public static IActivity Create()
    {
        var currentValue = new Variable<int?>();
        var start = new Variable<int>(1);
        var end = new Variable<int>(3);

        return new Sequence
        {
            Variables = { start, end },
            Activities =
            {
                new WriteLine(context => $"Counting numbers from {start.Get(context)} to {end.Get(context)}:"),
                new For
                {
                    Start = new Input<int>(start),
                    End = new Input<int>(end),
                    CurrentValue = new Output<MemoryReference?>(currentValue),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Current value: {currentValue.Get<int>(context)}"),
                            new If(context => currentValue.Get<int>(context) == 2)
                            {
                                Then = new Sequence
                                {
                                    Activities =
                                    {
                                        new WriteLine("Breaking out of the loop!"),
                                        new Break()
                                    }
                                }
                            }
                        }
                    }
                },
                new WriteLine("End of workflow")
            }
        };
    }
}