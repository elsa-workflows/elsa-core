using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Scenarios.SetGetVariables;

class SetGetVariablesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var variable = new Variable<string>("myvar", "test");

        workflow.Root = new Sequence
        {
            Variables = {
                variable
            },

            Activities =
            {
                new SetVariable<string>(variable,"Line 5"),
                new WriteLine(variable)
            }
        };
    }
}