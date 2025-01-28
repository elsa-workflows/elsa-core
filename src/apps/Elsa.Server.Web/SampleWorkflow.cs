using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Extensions;

namespace Elsa.Server.Web;

public class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WorkflowOptions.CommitStateOptions = new()
        {
            // Commit state before workflow starts executing.
            Starting = true,
            
            // Commit state before every activity that is about to execute.
            ActivityExecuted = true,
            
            // Commit state after every activity that executed. 
            ActivityExecuting = true,
        };
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Commit before executing").WithCommitStateBehavior(ActivityCommitStateBehavior.Executing),
                new WriteLine("Commit after executing").WithCommitStateBehavior(ActivityCommitStateBehavior.Executed),
                new WriteLine("Commit only based on the workflow commit options").WithCommitStateBehavior(ActivityCommitStateBehavior.Default),
                new WriteLine("Never commit the workflow when this activity is about to execute or has executed").WithCommitStateBehavior(ActivityCommitStateBehavior.Never),
            }
        };
    }
}