using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Activities.Workflows;

/// <summary>
/// Workflow demonstrating Break within a Fork branch.
/// </summary>
public class BreakInForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new Fork
                {
                    Branches =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch 1 executed"),
                                new Break()
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Branch 2 executed")
                            }
                        }
                    }
                },
                new WriteLine("After fork")
            }
        };
    }
}
