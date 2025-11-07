using Elsa.Workflows.Activities;
using Elsa.Workflows.IncidentStrategies;
using JetBrains.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.BulkDispatchWorkflows.Workflows;

[UsedImplicitly]
public class FaultingBulkChildWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WorkflowOptions.IncidentStrategyType = typeof(FaultStrategy);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Fault
                {
                    Message = new("Child workflow failed")
                }
            }
        };
    }
}
