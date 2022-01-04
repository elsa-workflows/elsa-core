using Elsa.Activities.Console;
using Elsa.Activities.Primitives;
using Elsa.Activities.Workflows;
using Elsa.Contracts;

namespace Elsa.Samples.Console1.Workflows;

public static class BlockingWorkflow
{
    public static IActivity Create()
    {
        return new Sequence(
            new WriteLine("Waiting for event..."),
            new Event("SomeEvent"), // Block here.
            new WriteLine("Resumed!"),
            new WriteLine("Done"));
    }
}