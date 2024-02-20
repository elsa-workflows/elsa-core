using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableExpressions;

class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var variable1 = workflow.WithVariable("Some variable");
        var variable2 = workflow.WithVariable(42);
        var literal1 = new Literal<string>("Some literal");
        var literal2 = new Literal<int>(84);
        var writeLine1 = new WriteLine(variable1);
        var writeLine2 = new WriteLine(literal1);
        var numberActivity1 = new NumberActivity(variable2);
        var numberActivity2 = new NumberActivity(literal2);

        workflow.Root = new Sequence
        {
            Activities =
            {
                writeLine1,
                writeLine2,
                numberActivity1,
                numberActivity2
            }
        };
    }
}   