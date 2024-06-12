using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionInputs;

class WriteMultiLineWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var favoriteFruit = new Variable<string>("banana");

        workflow.Root = new Sequence
        {
            Variables = { favoriteFruit },
            Activities =
            {
                new WriteMultiLine(new List<Input<string>>()
                {
                    new Input<string>(new Expression("Variable", favoriteFruit)),
                    new Input<string>("orange"),
                    new Input<string>(new Expression("JavaScript", "'apple';"))
                })
            }
        };
    }
}