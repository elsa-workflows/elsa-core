using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Extensions;
using Elsa.Workflows.CommitStates.Strategies;

namespace Elsa.Server.Web;

public class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WorkflowOptions.CommitStrategyName = "Every 10 seconds";
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Commit before executing").SetCommitStrategy(nameof(ExecutingActivityStrategy)),
                new WriteLine("Commit after executing").SetCommitStrategy(nameof(ExecutedActivityStrategy)),
                new WriteLine("Commit before & after executing").SetCommitStrategy(nameof(CommitAlwaysActivityStrategy)),
                new WriteLine("Commit only based on the workflow commit options").SetCommitStrategy(null),
                new WriteLine("Never commit the workflow when this activity is about to execute or has executed").SetCommitStrategy(nameof(CommitNeverActivityStrategy)),
            }
        };
    }
}