using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows.Workflows;

public class DispatchWithInputWorkflow : WorkflowBase
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
                    WorkflowDefinitionId = new(ChildWorkflowWithInput.DefinitionId),
                    Input = new(new Dictionary<string, object>
                    {
                        ["Message"] = "Hello from parent!"
                    }),
                    WaitForCompletion = new(true)
                },
                new WriteLine("Parent completed")
            }
        };
    }
}
