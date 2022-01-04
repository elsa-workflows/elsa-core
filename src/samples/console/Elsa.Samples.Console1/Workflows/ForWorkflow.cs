using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Samples.Console1.Workflows;

public static class ForWorkflow
{
    public static IActivity Create()
    {
        var currentValue = new Variable<int?>();
        var start = new Variable<int>(1);
        var end = new Variable<int>(3);

        var for1 = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            CurrentValue = currentValue,
            Body = new WriteLine(context => $"Current value: {currentValue.Get<int>(context)}")
        };

        return new Sequence
        {
            Variables = { start, end },
            Activities =
            {
                new WriteLine(context => $"Counting numbers from {start.Get(context)} to {end.Get(context)}:"),
                for1,
                new WriteLine("End of workflow")
            }
        };
    }
}