using Elsa.Extensions;
using Elsa.Workflows.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.BulkDispatchWorkflows.Workflows;

[UsedImplicitly]
public class FruitWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var item = builder.WithInput<string>("Item");
        builder.Root = new WriteLine(x => $"Mixing {x.GetInput<string>(item)}");
    }
}