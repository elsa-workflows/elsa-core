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
