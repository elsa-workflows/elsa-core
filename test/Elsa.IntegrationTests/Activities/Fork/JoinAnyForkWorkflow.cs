using Elsa.Activities;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

public class JoinAnyForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
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
                    JoinMode = JoinMode.WaitAny
                },
                new WriteLine("End")
            }
        });
    }
}