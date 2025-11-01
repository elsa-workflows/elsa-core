using Elsa.Workflows.Activities;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Runtime.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.BulkDispatch.Workflows;

[UsedImplicitly]
public class BulkDispatchInvalidDefinitionWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(BulkDispatchInvalidDefinitionWorkflow);
    public static readonly string InvalidChildWorkflowId = "NonExistentWorkflow";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WorkflowOptions.IncidentStrategyType = typeof(FaultStrategy);

        builder.Root = new Sequence
        {
            Activities =
            {
                new BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new(InvalidChildWorkflowId),
                    Items = new(() => new object[] { 1 }),
                    WaitForCompletion = new(true)
                }
            }
        };
    }
}
