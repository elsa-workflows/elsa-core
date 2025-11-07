using Elsa.Workflows.Activities;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Runtime.Activities;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows.Workflows;

[UsedImplicitly]
public class DispatchInvalidDefinitionWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    public static readonly string InvalidChildWorkflowId = "NonExistentWorkflow";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WorkflowOptions.IncidentStrategyType = typeof(FaultStrategy);

        builder.Root = new Sequence
        {
            Activities =
            {
                new DispatchWorkflow
                {
                    WorkflowDefinitionId = new(InvalidChildWorkflowId),
                    WaitForCompletion = new(true)
                }
            }
        };
    }
}
