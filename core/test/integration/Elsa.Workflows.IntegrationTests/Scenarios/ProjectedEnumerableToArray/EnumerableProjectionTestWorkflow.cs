using Elsa.Expressions.JavaScript.Activities;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ProjectedEnumerableToArray;

/// <summary>
/// Code-first workflow that reproduces the "projected IEnumerable to array" scenario end-to-end.
/// </summary>
public class EnumerableProjectionTestWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        // Store in workflow instance state to mimic real execution behavior.
        var messagesVar = new Variable<object>("Messages", Array.Empty<string>()).WithWorkflowStorage();

        workflow.WithVariables(messagesVar);

        workflow.Root = new Sequence
        {
            Activities =
            {
                new TestEnumerableActivity
                {
                    EnumerableResult = new(messagesVar)
                },

                new RunJavaScript
                {
                    // Accessing Messages through JS triggers the conversion logic.
                    Script = new("return getMessages();"),
                    Result = new(messagesVar)
                }
            }
        };
    }
}