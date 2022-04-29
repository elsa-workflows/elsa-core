using Elsa.Activities;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.JavaScript.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class RunJavaScriptWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        var scriptResult = new Variable();

        workflow.WithRoot(new Sequence
        {
            Variables = { scriptResult },
            Activities =
            {
                new RunJavaScript("1 + 1").CaptureOutput(scriptResult),
                new WriteLine(context => $"Result: {scriptResult.Get(context)}")
            }
        });
    }
}