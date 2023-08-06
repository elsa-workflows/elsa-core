using System;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Activities;

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