using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDeletion.Workflows;

/// <summary>
/// A simple workflow that suspends, used for testing workflow deletion.
/// </summary>
public class SuspendedWorkflowForDeletion : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Delay(TimeSpan.FromSeconds(60)), // Suspends for 60 seconds
                new WriteLine("After delay") // Should not execute if deleted
            }
        };
    }
}
