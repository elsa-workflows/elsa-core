using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.PublishEventOutbox;

public class PublishOrderShippedEventWorkflow : WorkflowBase
{
    public const string EventName = "OrderShipped";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new PublishEvent
                {
                    EventName = new(EventName)
                }
            }
        };
    }
}

public class ConsumeOrderShippedEventWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Event(PublishOrderShippedEventWorkflow.EventName)
                {
                    CanStartWorkflow = true
                }
            }
        };
    }
}
