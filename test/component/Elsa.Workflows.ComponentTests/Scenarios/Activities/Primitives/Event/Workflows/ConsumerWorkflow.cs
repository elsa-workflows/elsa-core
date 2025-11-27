using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event.Workflows;

/// <summary>
/// A workflow that listens for global events as a trigger and captures the payload.
/// </summary>
public class ConsumerWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        var eventPayload = new Variable<object>();

        workflow.WithDefinitionId(DefinitionId);
        workflow.WithVariable(eventPayload);
        workflow.WithOutput<object>("ReceivedPayload"); // Declare the output

        var eventActivity = new Runtime.Activities.Event("GlobalOrderEvent")
        {
            CanStartWorkflow = true,
            Result = new(eventPayload)
        };

        workflow.Root = new Sequence
        {
            Activities =
            {
                eventActivity,
                new SetOutput
                {
                    OutputName = new("ReceivedPayload"),
                    OutputValue = new(eventPayload)
                },
                new End()
            }
        };
    }
}
