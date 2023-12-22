using Elsa.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Activities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Workflows;

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