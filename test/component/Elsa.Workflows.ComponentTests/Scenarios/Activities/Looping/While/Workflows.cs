using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Looping.While;

public class WhileCounterWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(WhileCounterWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var counter = builder.WithVariable("Counter", 0);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.While(context => counter.Get(context) < 5)
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Iteration {counter.Get(context)}"),
                            new SetVariable<int>(counter, context => counter.Get(context) + 1)
                        }
                    }
                },
                new WriteLine("Loop completed")
            }
        };
    }
}

public class WhileFalseConditionWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(WhileFalseConditionWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.While(new Input<bool>(false))
                {
                    Body = new WriteLine("This should not execute")
                },
                new WriteLine("Loop completed")
            }
        };
    }
}

public class WhileWithBreakWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(WhileWithBreakWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var counter = builder.WithVariable("Counter", 0);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.While(new Input<bool>(true))
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Iteration {counter.Get(context)}"),
                            new SetVariable<int>(counter, context => counter.Get(context) + 1),
                            new If(context => counter.Get(context) >= 3)
                            {
                                Then = new Break()
                            }
                        }
                    }
                },
                new WriteLine("Loop completed")
            }
        };
    }
}


