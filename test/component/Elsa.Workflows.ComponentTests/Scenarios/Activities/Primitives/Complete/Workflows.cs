using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Complete;

public class CompleteWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(CompleteWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before complete"),
                new Elsa.Workflows.Activities.Complete(),
                new WriteLine("After complete - should not execute")
            }
        };
    }
}

public class CompleteWithOutputWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(CompleteWithOutputWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Processing..."),
                new Elsa.Workflows.Activities.Complete()
            }
        };
    }
}


