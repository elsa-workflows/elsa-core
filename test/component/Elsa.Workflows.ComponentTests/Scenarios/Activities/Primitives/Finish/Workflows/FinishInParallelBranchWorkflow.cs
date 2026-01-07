using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Finish.Workflows;

/// <summary>
/// A workflow where Finish is executed in one branch of a Fork while other branches have Event activities.
/// Tests that Finish clears completion callbacks from all branches.
/// </summary>
public class FinishInParallelBranchWorkflow : WorkflowBase
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
                new Fork
                {
                    JoinMode = ForkJoinMode.WaitAll,
                    Branches =
                    {
                        new Elsa.Workflows.Activities.Finish(),
                        new Runtime.Activities.Event("ShouldNotExecute")
                    }
                }
            }
        };
    }
}
