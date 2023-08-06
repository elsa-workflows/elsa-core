using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.ConsoleApp.ActivityOutput.Workflows;

/// <summary>
/// Demonstrates accessing the last result of a given activity.
/// </summary>
public class LastResultWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            // Setup the sequence of activities to run.
            Activities =
            {
                new WriteLine("===================================="),
                new WriteLine("Last Result Workflow"),
                new WriteLine("===================================="),
                new WriteLine("Please tell me your age:"),
                new ReadLine(),
                new If
                {
                    // If aged 18 or up, beer is provided, soda otherwise.
                    Condition = new(context => context.GetLastResult<int>() < 18),
                    Then = new WriteLine("Enjoy your soda!"),
                    Else = new WriteLine("Enjoy your beer!")
                },
                new WriteLine("Come again!")
            }
        };
    }
}