using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.ConsoleApp.ActivityOutput.Workflows;

/// <summary>
/// Demonstrates accessing the output of a given activity.
/// </summary>
public class TargetActivityOutputWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        // Create a variable to reference the activity so that we can reference its output.
        var readLine = new ReadLine();

        builder.Root = new Sequence
        {
            // Setup the sequence of activities to run.
            Activities =
            {
                new WriteLine("===================================="),
                new WriteLine("Target Activity Output Workflow"),
                new WriteLine("===================================="),
                new WriteLine("Please tell me your age:"),
                readLine,
                new If
                {
                    // If aged 18 or up, beer is provided, soda otherwise.
                    Condition = new(context => readLine.GetResult<int>(context) < 18),
                    Then = new WriteLine("Enjoy your soda!"),
                    Else = new WriteLine("Enjoy your beer!")
                },
                new WriteLine("Come again!")
            }
        };
    }
}