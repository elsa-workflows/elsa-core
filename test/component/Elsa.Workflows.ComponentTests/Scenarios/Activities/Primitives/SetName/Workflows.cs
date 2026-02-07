using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.SetName;

public class SetNameWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(SetNameWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.SetName { Value = new Input<string>("My Custom Workflow Name") },
                new WriteLine("Name set successfully")
            }
        };
    }
}

public class SetDynamicNameWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(SetDynamicNameWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var orderId = builder.WithVariable<string>("OrderId", "12345");

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.SetName(new Input<string>(context => $"Order-{orderId.Get(context)}")),
                new WriteLine("Dynamic name set successfully")
            }
        };
    }
}



