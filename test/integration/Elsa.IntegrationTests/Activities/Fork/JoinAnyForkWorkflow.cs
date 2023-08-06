using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.IntegrationTests.Activities;

public class JoinAnyForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new Fork
                {
                    Branches =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new Event("Event 1")
                                {
                                    Id = "Event1"
                                },
                                new WriteLine("Branch 1")
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new Event("Event 2")
                                {
                                    Id = "Event2"
                                },
                                new WriteLine("Branch 2")
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new Event("Event 3")
                                {
                                    Id = "Event3"
                                },
                                new WriteLine("Branch 3")
                            }
                        },
                    },
                    JoinMode = ForkJoinMode.WaitAny
                },
                new WriteLine("End")
            }
        };
    }
}