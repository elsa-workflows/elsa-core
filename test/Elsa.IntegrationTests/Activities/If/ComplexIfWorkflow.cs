using System;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

public class ComplexIfWorkflow : WorkflowBase
{
    private readonly Func<bool> _condition;

    public ComplexIfWorkflow(Func<bool> condition)
    {
        _condition = condition;
    }
        
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new If(_condition)
                {
                    Then = new Sequence(
                        
                        new WriteLine("Executing"),
                        new WriteLine("True!")
                    ),
                    Else = new Sequence(
                        
                        new WriteLine("Executing"),
                        new WriteLine("False!")
                    )
                },
                new WriteLine("End")
            }
        });
    }
}