using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DanglingActivityCancellation;

/// <summary>
/// Reproduces https://github.com/elsa-workflows/elsa-core/issues/7717.
/// A blocking approval branch runs in parallel with a self-looping reminder branch.
/// When the approval branch completes through a terminal node, the reminder branch is canceled.
/// </summary>
class ApprovalWithReminderWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var start = new Start
        {
            Id = "Start"
        };
        var approval = new Event("Approval")
        {
            Id = "Approval"
        };
        var reminder = new Event("Reminder")
        {
            Id = "Reminder"
        };
        var end = new End
        {
            Id = "End"
        };

        workflow.Root = new Flowchart
        {
            Activities =
            {
                start,
                approval,
                reminder,
                end
            },
            Connections =
            {
                new(start, approval),
                new(approval, end),
                new(start, reminder),
                new(reminder, reminder) // self-loop (periodic reminder)
            }
        };
    }
}
