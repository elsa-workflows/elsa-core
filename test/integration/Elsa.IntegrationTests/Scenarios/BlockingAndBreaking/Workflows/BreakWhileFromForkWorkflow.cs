using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.IntegrationTests.Scenarios.BlockingAndBreaking.Workflows;

public class BreakWhileFromForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentValue = new Variable<int?>(0);

        workflow.WithVariable(currentValue);

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new While(() => true)
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
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

                                            // This should break the while loop, not matter how high up in the tree it is.
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
                },
                new WriteLine("End")
            }
        };
    }
}