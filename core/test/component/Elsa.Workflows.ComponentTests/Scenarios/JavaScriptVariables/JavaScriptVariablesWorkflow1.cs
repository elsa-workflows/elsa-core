using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.JavaScriptVariables;

public class JavaScriptVariablesWorkflow1 : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.WithVariable("MagicNumber", 3).WithWorkflowStorage();
        builder.Root = new RunJavaScript("setMagicNumber(42)", null, null);
    }
}