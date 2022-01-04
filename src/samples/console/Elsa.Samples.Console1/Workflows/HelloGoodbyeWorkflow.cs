using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;

namespace Elsa.Samples.Console1.Workflows;

public static class HelloGoodbyeWorkflow
{
    public static IActivity Create() =>
        new Sequence(
            new WriteLine("Hello World!"),
            new WriteLine("Goodbye cruel world...")
        );
}