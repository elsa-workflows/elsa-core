using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class RaceJoinBookmarkTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Token mode: an activity after a WaitAny join can create a bookmark and resume")]
    public async Task WaitAny_join_followed_by_bookmark_resumes()
    {
        await _fixture.BuildAsync();
        var workflow = await _fixture.Services.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder().BuildWorkflowAsync<RaceJoinBookmarkWorkflow>();
        var runner = _fixture.Services.GetRequiredService<IWorkflowRunner>();

        var state = (await runner.RunAsync(workflow, new RunWorkflowOptions().WithTokenBasedFlowchart())).WorkflowState;
        state = await ResumeAsync(runner, workflow, state, "Remind");

        Assert.Equal(WorkflowStatus.Running, state.Status);
        Assert.Contains(state.Bookmarks, b => b.ActivityId == "After");
        Assert.DoesNotContain(state.Bookmarks, b => b.ActivityId == "Pay");

        state = await ResumeAsync(runner, workflow, state, "After");

        Assert.Equal(new[] { "Start", "End" }, _fixture.CapturingTextWriter.Lines);
        Assert.Equal(WorkflowStatus.Finished, state.Status);
        Assert.Equal(WorkflowSubStatus.Finished, state.SubStatus);
    }

    [Fact(DisplayName = "Token mode: a WaitAny join that loops back re-opens its waits and resumes")]
    public async Task WaitAny_join_looping_back_to_bookmarks_resumes()
    {
        await _fixture.BuildAsync();
        var workflow = await _fixture.Services.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder().BuildWorkflowAsync<RaceJoinBookmarkLoopWorkflow>();
        var runner = _fixture.Services.GetRequiredService<IWorkflowRunner>();

        var state = (await runner.RunAsync(workflow, new RunWorkflowOptions().WithTokenBasedFlowchart())).WorkflowState;
        var firstPassBookmarkIds = state.Bookmarks.Select(b => b.Id).ToList();
        state = await ResumeAsync(runner, workflow, state, "Remind");

        // Both waits are re-opened by the loop: new bookmarks, none left over from the first pass.
        Assert.Equal(WorkflowStatus.Running, state.Status);
        Assert.Single(state.Bookmarks, b => b.ActivityId == "Pay");
        Assert.Single(state.Bookmarks, b => b.ActivityId == "Remind");
        Assert.DoesNotContain(state.Bookmarks, b => firstPassBookmarkIds.Contains(b.Id));

        state = await ResumeAsync(runner, workflow, state, "Pay");

        Assert.Equal(new[] { "Start", "End" }, _fixture.CapturingTextWriter.Lines);
        Assert.Equal(WorkflowStatus.Finished, state.Status);
        Assert.Equal(WorkflowSubStatus.Finished, state.SubStatus);
    }

    private static async Task<State.WorkflowState> ResumeAsync(IWorkflowRunner runner, Workflow workflow, State.WorkflowState state, string activityId)
    {
        var bookmark = state.Bookmarks.Single(b => b.ActivityId == activityId);
        var options = new RunWorkflowOptions { BookmarkId = bookmark.Id }.WithTokenBasedFlowchart();
        return (await runner.RunAsync(workflow, state, options)).WorkflowState;
    }
}
