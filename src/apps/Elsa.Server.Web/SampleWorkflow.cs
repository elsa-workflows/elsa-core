using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Extensions;
using Elsa.Workflows.CommitStates.Strategies.Activities;

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
                new WriteLine("Commit before executing").WithCommitStateStrategy(nameof(ExecutingActivityStrategy)),
                new WriteLine("Commit after executing").WithCommitStateStrategy(nameof(ExecutedActivityStrategy)),
                new WriteLine("Commit before & after executing").WithCommitStateStrategy(nameof(CommitAlwaysActivityStrategy)),
                new WriteLine("Commit only based on the workflow commit options").WithCommitStateStrategy(nameof(DefaultActivityStrategy)),
                new WriteLine("Never commit the workflow when this activity is about to execute or has executed").WithCommitStateStrategy(nameof(CommitNeverActivityStrategy)),
            }
        };
    }
}