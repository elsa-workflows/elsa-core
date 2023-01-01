using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

public class BreakWhileForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentValue = new Variable<int?>("CurrentValue", 0);

        workflow.Root = new Sequence
        {
            Variables = { currentValue },
            Activities =
            {
                While.True(new Fork
                {
                    Branches =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new SetVariable
                                {
                                    Variable = currentValue,
                                    Value = new Input<object?>(context => currentValue.Get(context) + 1)
                                },
                                new If(context => currentValue.Get(context) == 3)
                                {
                                    Then = new Break()
                                }
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new WriteLine("Waiting for event..."),
                                new Event("Some event") { Id = "SomeEvent" },
                                new WriteLine("Resuming"),
                            }
                        }
                    }
                }),
            }
        };
    }
}