using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;
using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows.Workflows;

[UsedImplicitly]
public class ChildWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString(); 
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new Delay(TimeSpan.FromMilliseconds(250)),
                new WriteLine("Hello from Child!")
            }
        };
    }
}