using Elsa.Expressions.JavaScript.Models;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptUnderscoreVariables;

public class UnderscoreVariableWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(Guid.NewGuid().ToString());
        workflow.WithVariable("meting_context", new Dictionary<string, object?> { ["beschrijving"] = "Cliënt gevallen" });

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(JavaScriptExpression.Create("variables.meting_context.beschrijving"))
            }
        };
    }
}

public class DollarSignVariableWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(Guid.NewGuid().ToString());
        workflow.WithVariable("$amount", "10");
        workflow.WithVariable("total$amount", "20");

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(JavaScriptExpression.Create("variables.$amount")),
                new WriteLine(JavaScriptExpression.Create("variables.total$amount"))
            }
        };
    }
}

public class DigitLeadingVariableWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(Guid.NewGuid().ToString());
        workflow.WithVariable("1234", "Numeric name");

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(JavaScriptExpression.Create("get1234()"))
            }
        };
    }
}
