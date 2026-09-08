using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DanglingActivityCancellation;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    /// <summary>
    /// Reproduces https://github.com/elsa-workflows/elsa-core/issues/7717.
    /// When the workflow is resumed via a trigger (which sets <see cref="WorkflowExecutionContext.TriggerActivityId"/>)
    /// and the blocking branch completes through a terminal node, the parallel self-looping branch is canceled.
    /// The canceled activity is "dangling" relative to the trigger-rooted flow graph, which previously faulted the workflow.
    /// </summary>
    [Fact(DisplayName = "Canceling a dangling parallel branch on trigger-resume does not fault the workflow")]
    public async Task CancelingDanglingParallelBranchDoesNotFault()
    {
        await _services.PopulateRegistriesAsync();

        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<ApprovalWithReminderWorkflow>();

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
