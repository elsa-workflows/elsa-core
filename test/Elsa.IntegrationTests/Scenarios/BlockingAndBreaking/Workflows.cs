using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Scenarios.BlockingAndBreaking;

public class BreakWhileBlockForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentValue = new Variable<int?>(0);

        workflow.WithVariable(currentValue);
        
        workflow.WithRoot(new While(() => true)
        {
            Body =
                new Sequence
                {
                    Activities =
                    {
                        new Fork
                        {
                            JoinMode = JoinMode.WaitAll,
                            Branches =
                            {
                                new Sequence
                                {
                                    Activities =
                                    {
                                        new WriteLine("Branch 1"),
                                        new Event("Branch 1") { Id = "Branch 1" },
                                        new WriteLine("Branch 1 - Resumed"),
                                        new Break()
                                    }
                                },
                                new Sequence
                                {
                                    Activities =
                                    {
                                        new WriteLine("Branch 2"),
                                        new Event("Branch 2") { Id = "Branch 2" },
                                        new WriteLine("Branch 2 - Resumed"),
                                    }
                                }
                            }
                        }
                    }
                }
        });
    }
}