using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class IfThenWorkflow : WorkflowBase
{
    private readonly Func<bool> _condition;

    public IfThenWorkflow(Func<bool> condition)
    {
        _condition = condition;
    }
        
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new If(_condition)
        {
            Then = new WriteLine("True!"),
            Else = new WriteLine("False!")
        };
    }
}