using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Cron.Workflows;

public class CronWorkflow : WorkflowBase
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
                Elsa.Scheduling.Activities.Cron.FromCronExpression("* * * * * *"), // Every second
                new WriteLine("End") { Id = "WriteLine2" }
            }
        };
    }
}
