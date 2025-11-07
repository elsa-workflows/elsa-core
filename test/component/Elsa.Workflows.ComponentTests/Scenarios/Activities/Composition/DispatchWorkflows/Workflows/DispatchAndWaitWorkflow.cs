using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows.Workflows;

public class DispatchAndWaitWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow
                {
                    WorkflowDefinitionId = new(ChildWorkflow.DefinitionId),
                    WaitForCompletion = new(true)
                },
                new WriteLine("Parent completed")
            }
        };
    }
}