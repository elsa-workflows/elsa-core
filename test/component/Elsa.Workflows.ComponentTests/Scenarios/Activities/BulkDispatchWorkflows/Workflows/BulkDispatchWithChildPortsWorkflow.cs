using Elsa.Extensions;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.BulkDispatchWorkflows.Workflows;

public class BulkDispatchWithChildPortsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var completedCountVariable = builder.WithVariable("CompletedCount", 0).WithWorkflowStorage();
        var faultedCountVariable = builder.WithVariable("FaultedCount", 0).WithWorkflowStorage();

        builder.Root = new Runtime.Activities.BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(FaultingChildWorkflow.DefinitionId),
            Items = new(new object[] { 1, 2, 3 }),
            WaitForCompletion = new(true),
            ChildCompleted = new Sequence
            {
                Activities =
                {
                    new SetVariable
                    {
                        Variable = completedCountVariable,
                        Value = new(context => completedCountVariable.Get(context) + 1)
                    }
                }
            },
            ChildFaulted = new Sequence
            {
                Activities =
                {
                    new SetVariable
                    {
                        Variable = faultedCountVariable,
                        Value = new(context => faultedCountVariable.Get(context) + 1)
                    }
                }
            }
        };
    }
}
