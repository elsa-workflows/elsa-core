using System;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

public class IfThenWorkflow : WorkflowBase
{
    private readonly Func<bool> _condition;

    public IfThenWorkflow(Func<bool> condition)
    {
        _condition = condition;
    }
        
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new If(_condition)
        {
            Then = new WriteLine("True!"),
            Else = new WriteLine("False!")
        });
    }
}