using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class ComplexIfWorkflow : WorkflowBase
{
    private readonly Func<bool> _condition;

    public ComplexIfWorkflow(Func<bool> condition)
    {
        _condition = condition;
    }
        
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
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
                            new WriteLine("True!")
                        }
                    },
                    Else = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("Executing"),
                            new WriteLine("False!")
                        }
                    }
                },
                new WriteLine("End")
            }
        };
    }
}