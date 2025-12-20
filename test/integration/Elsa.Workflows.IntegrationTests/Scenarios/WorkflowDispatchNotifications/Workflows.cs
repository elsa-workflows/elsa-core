using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDispatchNotifications;

public class SimpleWorkflow : WorkflowBase
{
    public static string DefinitionId = "SimpleWorkflow";
    
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.DefinitionId = DefinitionId;
        builder.Root = new WriteLine("Hello from SimpleWorkflow");
    }
}
