using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Console3.Workflows;

public class HelloWorld : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Hello World!"),
                new WriteLine("Goodbye cruel world...")
            }
        };
    }
}