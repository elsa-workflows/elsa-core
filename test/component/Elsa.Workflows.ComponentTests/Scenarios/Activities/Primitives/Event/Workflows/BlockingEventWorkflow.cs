using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event.Workflows;

public class BlockingEventWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);
        workflow.Root = new Sequence
        {
            Activities =
            {
                new Start(),
                new Runtime.Activities.Event("Order Shipped"),
                new End()
            }
        };
    }
}