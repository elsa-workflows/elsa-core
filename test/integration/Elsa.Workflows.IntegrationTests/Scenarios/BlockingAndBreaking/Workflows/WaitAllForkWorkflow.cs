using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BlockingAndBreaking.Workflows;

public class WaitAllForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentValue = new Variable<int?>(0);

        workflow.WithVariable(currentValue);

        workflow.Root =
            new Sequence
            {
                Activities =
                {
                    new WriteLine("Start"),
                    new Fork
                    {
                        JoinMode = ForkJoinMode.WaitAll,
                        Branches =
                        {
                            new Sequence
                            {
                                Activities =
                                {
                                    new WriteLine("Branch 1"),
                                    new Event("Branch 1") { Id = "Branch 1" },
                                    new WriteLine("Branch 1 - Resumed"),
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
                    },
                    new WriteLine("End")
                }
            };
    }
}