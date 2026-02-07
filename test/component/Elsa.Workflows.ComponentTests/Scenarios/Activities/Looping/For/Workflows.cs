using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Looping.For;

public class ForBasicWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ForBasicWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var currentValue = builder.WithVariable<int>("CurrentValue", 0);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.For(0, 4, 1)  // 0-4 inclusive = 5 iterations
                {
                    CurrentValue = new(currentValue),
                    Body = new WriteLine(context => $"Iteration {currentValue.Get(context)}")
                },
                new WriteLine("Loop completed")
            }
        };
    }
}

public class ForWithStepWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ForWithStepWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var currentValue = builder.WithVariable<int>("CurrentValue", 0);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.For(0, 4, 2)  // 0, 2, 4 = 3 iterations
                {
                    CurrentValue = new(currentValue),
                    Body = new WriteLine(context => $"Iteration {currentValue.Get(context)}")
                },
                new WriteLine("Loop completed")
            }
        };
    }
}

public class ForZeroIterationsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(ForZeroIterationsWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                // Use exclusive bound with start == end to get zero iterations
                new Elsa.Workflows.Activities.For(0, 0, 1)
                {
                    OuterBoundInclusive = new Input<bool>(false),  // 0 to 0 exclusive = 0 iterations
                    Body = new WriteLine("This should not execute")
                },
                new WriteLine("Loop completed")
            }
        };
    }
}


