using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;

[UsedImplicitly]
public class BulkChildWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var itemVariable = new Variable<object>("Item", "Item");
        builder.WithVariable(itemVariable);

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(context => $"Processing item: {itemVariable.Get(context)}")
            }
        };
    }
}
