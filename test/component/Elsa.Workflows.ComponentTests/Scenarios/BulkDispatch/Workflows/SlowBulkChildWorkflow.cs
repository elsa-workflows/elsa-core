using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;

[UsedImplicitly]
public class SlowBulkChildWorkflow : WorkflowBase
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
                Delay.FromMilliseconds(10),
                new WriteLine(context => $"Processing item: {context.GetInput<string>(item)}")
            }
        };
    }
}
