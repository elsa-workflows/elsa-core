using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;

public class ResumeDispatchWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new PublishEvent
                {
                    EventName = new Input<string>("ResumeBlockDispatch")
                }
            }
        };
    }
}