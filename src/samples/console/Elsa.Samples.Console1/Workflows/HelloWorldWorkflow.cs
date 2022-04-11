using Elsa.Contracts;
using Elsa.Modules.Activities.Activities.Console;

namespace Elsa.Samples.Console1.Workflows;

public static class HelloWorldWorkflow
{
    public static IActivity Create() => new WriteLine("Hello World!") { Id = "WriteLine1" };
}