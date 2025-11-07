using Elsa.Extensions;
using Elsa.Workflows.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows.Workflows;

[UsedImplicitly]
public class ChildWorkflowWithInput : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var message = builder.WithInput<string>("Message");

        builder.Root = new WriteLine(context => $"Received: {context.GetInput<string>(message)}");
    }
}
