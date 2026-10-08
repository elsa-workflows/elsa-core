using Elsa.Extensions;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Workflows;

/// <summary>
/// A simple workflow for testing distributed lock resilience.
/// </summary>
public class SimpleWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Workflow execution started"),
                new WriteLine("Workflow execution completed")
            }
        };
    }
}
