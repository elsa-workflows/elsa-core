using Elsa.JavaScript.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class RunJavaScriptWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var scriptResult = new Variable();

        workflow.WithRoot(new Sequence
        {
            Variables = { scriptResult },
            Activities =
            {
                new RunJavaScript("1 + 1").CaptureResult(scriptResult),
                new WriteLine(context => $"Result: {scriptResult.Get(context)}")
            }
        });
    }
}