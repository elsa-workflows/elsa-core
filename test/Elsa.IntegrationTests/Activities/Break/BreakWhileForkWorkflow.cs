using Elsa.Activities;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Activities.Primitives;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

public class BreakWhileForkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        var currentValue = new Variable<int?>(0);

        workflow.WithRoot(new Sequence
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
        });
    }
}