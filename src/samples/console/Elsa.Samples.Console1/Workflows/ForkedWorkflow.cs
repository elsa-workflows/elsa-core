using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Samples.Console1.Workflows;

public static class ForkedWorkflow
{
    public static IActivity Create()
    {
        return new Sequence(
            new WriteLine("Forking..."),
            new Fork
            {
                JoinMode = new Input<JoinMode>(JoinMode.WaitAll),
                Branches =
                {
                    new Sequence(
                        new WriteLine("Branch 1 (blocking)"),
                        new Event("Branch1"),
                        new WriteLine("Branch 1 (resumed)"),
                        new WriteLine("Done!")),
                    new WriteLine("Branch 2"),
                    new WriteLine("Branch 3")
                }
            });
    }
}