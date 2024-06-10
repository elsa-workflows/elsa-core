using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities.Workflows;

public class DeleteWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString(); 
    public static readonly string Type = nameof(DeleteWorkflow); 
    
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