using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionInputs;
class DynamicArgumentsWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var nameVariable = new Variable<string>("name", "Frank");

        workflow.Root = new Sequence
        {
            Variables = { nameVariable },
            Activities =
            {
                new DynamicArguments(new Dictionary<string, Input<object>>()
                {
                    { "name", new Input<object>(new Expression("JavaScript", "getVariable('name')")) },
                    { "isAdmin", new Input<object>(false) },
                    { "age", new Input<object>(new Expression("JavaScript", "let age = 30 + 12; age;")) }
                })
            }
        };
    }
}