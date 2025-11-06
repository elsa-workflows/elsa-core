using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows.Workflows;

[UsedImplicitly]
public class SlowChildWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                Delay.FromMilliseconds(10),
                new WriteLine("Slow child workflow executed")
            }
        };
    }
}
