using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.Blocking;

class BlockingSequentialWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Line 1"),
                new Event("Resume"){ Id = "Resume"},
                new WriteLine("Line 2"),
                new WriteLine("Line 3")
            }
        };
    }
}