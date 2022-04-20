using System;
using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;

namespace Elsa.IntegrationTests.Activities;

public class ComplexIfWorkflow : IWorkflow
{
    private readonly Func<bool> _condition;

    public ComplexIfWorkflow(Func<bool> condition)
    {
        _condition = condition;
    }
        
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new If(_condition)
                {
                    Then = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("Executing"),
                            new WriteLine("True!"),
                        }
                    },
                    Else = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("Executing"),
                            new WriteLine("False!"),
                        }
                    }
                },
                new WriteLine("End")
            }
        });
    }
}