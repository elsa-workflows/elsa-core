using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultWorkflowCommitStrategy;

/// <summary>
/// A simple workflow that does not specify an explicit commit strategy.
/// </summary>
public class SimpleWorkflowWithoutWorkflowCommitStrategy : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Activity 1"),
                new WriteLine("Activity 2"),
                new WriteLine("Activity 3")
            }
        };
    }
}
