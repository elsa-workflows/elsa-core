using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event.Workflows;

/// <summary>
/// A workflow that publishes an event to itself (local event).
/// </summary>
public class PublishAndConsumeEventWorkflow : WorkflowBase
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
                // Publish a local event
                new PublishEvent
                {
                    EventName = new("LocalOrderEvent"),
                    IsLocalEvent = new(true),
                    Payload = new(new { OrderId = 123 })
                },
                // Wait for the local event
                new Elsa.Workflows.Runtime.Activities.Event("LocalOrderEvent"),
                new End()
            }
        };
    }
}
