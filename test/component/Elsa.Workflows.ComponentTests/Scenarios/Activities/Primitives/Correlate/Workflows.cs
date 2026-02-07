using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Correlate;

public class CorrelateWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(CorrelateWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.Correlate { CorrelationId = new Input<string>("my-correlation-id") },
                new WriteLine("Correlation ID set")
            }
        };
    }
}

public class CorrelateUpdateWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(CorrelateUpdateWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        builder.Root = new Sequence
        {
            Activities =
            {
                new Elsa.Workflows.Activities.Correlate { CorrelationId = new Input<string>("initial-correlation-id") },
                new WriteLine("First correlation set"),
                new Elsa.Workflows.Activities.Correlate { CorrelationId = new Input<string>("updated-correlation-id") },
                new WriteLine("Correlation ID updated")
            }
        };
    }
}


