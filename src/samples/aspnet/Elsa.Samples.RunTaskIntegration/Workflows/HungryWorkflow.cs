using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Samples.RunTaskIntegration.Workflows;

public class HungryWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Event("Hungry"),
                new WriteLine("Hunger detected!"),
                new RunTask()
            }
        };
    }
}