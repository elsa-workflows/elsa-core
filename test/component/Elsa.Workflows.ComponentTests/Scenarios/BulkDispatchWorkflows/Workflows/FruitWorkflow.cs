using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Activities;
using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows.Workflows;

[UsedImplicitly]
public class FruitWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var item = builder.WithInput<string>("Item");
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(x => $"Mixing {x.GetInput<string>(item)}"),
                new TriggerSignal(x => x.GetInput<string>(item))
            }
        };
    }
}