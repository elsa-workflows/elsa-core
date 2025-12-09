using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultActivityCommitStrategy;

public class SimpleWorkflowWithoutActivityCommitStrategy : WorkflowBase
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
