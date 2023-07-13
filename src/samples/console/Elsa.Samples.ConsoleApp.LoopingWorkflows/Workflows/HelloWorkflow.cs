using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.ConsoleApp.LoopingWorkflows.Workflows;

/// <summary>
/// Demonstrates dynamic workflow creation. 
/// </summary>
public static class HelloWorkflow
{
    public static IActivity Create()
    {
        var messages = new[] { "Hello, world!", "Goodbye, cruel world..." };

        return new Workflow
        {
            Root = new Sequence
            {
                Activities = messages.Select(x => new WriteLine(x)).ToArray()
            }
        };
    }
}