using Elsa.Extensions;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Scenarios.SetGetVariables;

class SetGetVariablesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var variable = new Variable<string>("test");

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

class SetGetNamedVariablesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {    
            Activities =
            {
                new SetVariable()
                {
                    Variable = new Variable("MyVar"),
                    Value = new ("Some value")
                },
                new SetVariable()
                {
                    Variable = new Variable("some_other_variable"),
                    Value = new Input<object?>(new Variable("MyVar"))
                },
                new WriteLine(context => $"Other variable: {context.GetVariable<string>("MyVar")}")
            }
        };
    }
}
