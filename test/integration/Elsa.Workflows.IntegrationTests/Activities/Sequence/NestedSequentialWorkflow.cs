using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.IntegrationTests.Activities;

class NestedSequentialWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("Line 1"),
                        new WriteLine("Line 2"),
                        new WriteLine("Line 3")
                    }
                },
                new WriteLine("End"),
            }
        };
    }
}