using Elsa.Expressions.JavaScript.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

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

public class CollidingAccessorWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(Guid.NewGuid().ToString());
        // Explicit IDs: the name-derived default ID camelizes both names to the same value.
        workflow.WithVariable(new Variable<string>("fooBar", "legacy") { Id = "legacyVariable" });
        workflow.WithVariable(new Variable<string>("foo_bar", "new") { Id = "underscoreVariable" });

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(JavaScriptExpression.Create("getFooBar()")),
                new WriteLine(JavaScriptExpression.Create("setFooBar('updated'); variables.foo_bar + ',' + getFooBar()")),
                new WriteLine(JavaScriptExpression.Create("variables.foo_bar"))
            }
        };
    }
}
