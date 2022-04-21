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

        workflow.WithRoot(new Sequence
        {
            Variables = { currentValue },
            Activities =
            {
                While.True(new Sequence
                {
                    Activities =
                    {
                        new WriteLine(context => $"Current value: {currentValue.Get<int>(context)}"),
                        new Fork
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
                                        },
                                        new WriteLine("Branch 1"),
                                    }
                                },
                                new Sequence
                                {
                                    Activities =
                                    {
                                        new WriteLine("Blocking Branch 2"),
                                        new Event("Some event") { Id = "SomeEvent" },
                                        new WriteLine("Resuming Branch 2"),
                                    }
                                }
                            }
                        }
                    }
                }),
            }
        });
    }
}