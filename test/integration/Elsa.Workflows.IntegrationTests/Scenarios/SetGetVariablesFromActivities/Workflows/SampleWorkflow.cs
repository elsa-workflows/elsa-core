using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Workflows;

class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var variable1 = new Variable<string>();

        workflow.Root = new Sequence
        {
            Variables =
            {
                variable1
            },

            Activities =
            {
                new CreateVariableActivity(),
                new ReadVariableActivity
                {
                    Result = new(variable1)
                },
                new WriteLine(variable1)
            }
        };
    }
}