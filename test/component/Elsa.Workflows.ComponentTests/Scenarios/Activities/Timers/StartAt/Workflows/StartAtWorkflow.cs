using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.StartAt.Workflows;

public class StartAtWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Starting...") { Id = "WriteLine1" },
                new Elsa.Scheduling.Activities.StartAt(DateTimeOffset.UtcNow.AddSeconds(1)),
                new WriteLine("End") { Id = "WriteLine2" }
            }
        };
    }
}
