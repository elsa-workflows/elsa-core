using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.If;

public class IfTrueWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(IfTrueWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.If(new Input<bool>(true))
                {
                    Then = new WriteLine("Condition is true"),
                    Else = new WriteLine("Condition is false")
                }
            }
        };
    }
}

public class IfFalseWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(IfFalseWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.If(new Input<bool>(false))
                {
                    Then = new WriteLine("Condition is true"),
                    Else = new WriteLine("Condition is false")
                }
            }
        };
    }
}

public class IfNoElseWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(IfNoElseWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.If(new Input<bool>(false))
                {
                    Then = new WriteLine("Condition is true")
                    // No Else branch
                }
            }
        };
    }
}


