using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Workflows;

public class CompletingWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = "completing-workflow-test";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Starting workflow"),
                new WriteLine("Completing immediately")
            }
        };
    }
}
