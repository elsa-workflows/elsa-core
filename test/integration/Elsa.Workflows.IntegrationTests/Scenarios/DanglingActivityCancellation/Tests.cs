using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DanglingActivityCancellation;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    /// <summary>
    /// Reproduces https://github.com/elsa-workflows/elsa-core/issues/7717.
    /// A self-looping branch (e.g. a periodic reminder) runs in parallel with a blocking branch.
    /// When the workflow is resumed via a trigger (which sets <see cref="WorkflowExecutionContext.TriggerActivityId"/>)
    /// and the blocking branch completes through a terminal node, the parallel branch is canceled.
    /// The canceled activity is "dangling" relative to the trigger-rooted flow graph, which previously caused a fault.
    /// </summary>
    [Fact(DisplayName = "Canceling a dangling parallel branch on trigger-resume does not fault the workflow")]
    public async Task CancelingDanglingParallelBranchDoesNotFault()
    {
        await _services.PopulateRegistriesAsync();

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

        var workflow = new Workflow
        {
            Root = new Flowchart
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
                    new(reminder, reminder)
                }
            }
        };

        // Start the workflow. Both the approval and the (self-looping) reminder branches block.
        var result = await _workflowRunner.RunAsync(workflow);
        Assert.Equal(WorkflowSubStatus.Suspended, result.WorkflowState.SubStatus);

        var approvalBookmark = result.WorkflowState.Bookmarks.First(x => x.ActivityId == "Approval");

        // Resume the approval branch as if triggered (this is what e.g. the HTTP endpoint middleware does).
        var runOptions = new RunWorkflowOptions
        {
            BookmarkId = approvalBookmark.Id,
            TriggerActivityId = "Approval"
        };
        var resumeResult = await _workflowRunner.RunAsync(workflow, result.WorkflowState, runOptions);

        Assert.Equal(WorkflowStatus.Finished, resumeResult.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumeResult.WorkflowState.SubStatus);
    }
}
