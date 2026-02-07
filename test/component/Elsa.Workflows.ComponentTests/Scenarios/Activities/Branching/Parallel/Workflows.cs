using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.Parallel;

public class ParallelBasicWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ParallelBasicWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.Parallel
                {
                    Activities =
                    {
                        new WriteLine("Branch 1"),
                        new WriteLine("Branch 2"),
                        new WriteLine("Branch 3")
                    }
                },
                new WriteLine("All branches completed")
            }
        };
    }
}

public class ParallelWithSequencesWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ParallelWithSequencesWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.Parallel
                {
                    Activities =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch 1 - Step 1"),
                                new WriteLine("Branch 1 - Step 2")
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch 2 - Step 1"),
                                new WriteLine("Branch 2 - Step 2")
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch 3 - Step 1"),
                                new WriteLine("Branch 3 - Step 2")
                            }
                        }
                    }
                },
                new WriteLine("All branches completed")
            }
        };
    }
}

