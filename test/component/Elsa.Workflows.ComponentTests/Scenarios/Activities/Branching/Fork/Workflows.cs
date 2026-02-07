using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.Fork;

public class ForkBasicWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ForkBasicWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Elsa.Workflows.Activities.Fork
        {
            Branches =
            {
                new WriteLine("Branch A"),
                new WriteLine("Branch B"),
                new WriteLine("Branch C")
            }
        };
    }
}

public class ForkWaitAllWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ForkWaitAllWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.Fork
                {
                    JoinMode = ForkJoinMode.WaitAll,
                    Branches =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch A - Step 1"),
                                new WriteLine("Branch A - Step 2")
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch B - Step 1"),
                                new WriteLine("Branch B - Step 2")
                            }
                        }
                    }
                },
                new WriteLine("All branches completed")
            }
        };
    }
}

