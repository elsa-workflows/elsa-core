using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities.Workflows;

public class DeleteWorkflowClustered : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString(); 
    public static readonly string Type = nameof(DeleteWorkflowClustered); 
    
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Name = Type;
        builder.WithDefinitionId(DefinitionId);
        builder.WorkflowOptions.UsableAsActivity = true;
        builder.Root = new Sequence
        {
            Activities =
            {
                new Delay(TimeSpan.FromMilliseconds(250)),
                new WriteLine("This workflow will be deleted!")
            }
        };
    }
}