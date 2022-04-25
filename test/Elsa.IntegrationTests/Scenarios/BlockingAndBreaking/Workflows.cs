using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Activities.Primitives;

namespace Elsa.IntegrationTests.Scenarios.BlockingAndBreaking;

public class BreakWhileBlockForkWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
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
                                new Sequence{
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