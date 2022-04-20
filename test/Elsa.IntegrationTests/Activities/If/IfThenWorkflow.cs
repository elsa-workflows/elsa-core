using System;
using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Modules.Activities.Activities.Console;

namespace Elsa.IntegrationTests.Activities;

public class IfThenWorkflow : IWorkflow
{
    private readonly Func<bool> _condition;

    public IfThenWorkflow(Func<bool> condition)
    {
        _condition = condition;
    }
        
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new If(_condition)
        {
            Then = new WriteLine("True!"),
            Else = new WriteLine("False!")
        });
    }
}