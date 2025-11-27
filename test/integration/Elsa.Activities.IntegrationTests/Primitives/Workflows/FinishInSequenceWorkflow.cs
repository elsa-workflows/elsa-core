using Elsa.Workflows;
using Elsa.Workflows.Activities;
using System;

namespace Elsa.Activities.IntegrationTests.Primitives.Workflows;

public class FinishInSequenceWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before Finish"),
                new Finish(),
                new WriteLine("After Finish")
            }
        };
    }
}
