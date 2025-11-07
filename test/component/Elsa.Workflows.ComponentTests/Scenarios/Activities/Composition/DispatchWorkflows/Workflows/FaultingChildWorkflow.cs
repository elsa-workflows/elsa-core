using Elsa.Workflows.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows.Workflows;

[UsedImplicitly]
public class FaultingChildWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Fault
        {
            Message = new("Child workflow failed intentionally")
        };
    }
}
