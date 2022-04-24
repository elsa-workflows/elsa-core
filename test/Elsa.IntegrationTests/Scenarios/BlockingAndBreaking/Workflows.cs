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
        workflow.WithRoot(new While(context => currentValue.Get(context) < 3)
        {
            Body =
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine(context => $"Current value: {currentValue.Get<int>(context)}"),
                        new SetVariable<int?>(currentValue, context => currentValue.Get(context) + 1),
                        new Fork
                        {
                            Branches =
                            {
                                new WriteLine("Branch 1"),
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
                }
        });
    }
}