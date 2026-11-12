using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Core;

/// <summary>
/// Verifies that the scheduling chain of active activity execution contexts survives a suspend/resume cycle.
/// </summary>
public class CallStackPersistenceTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _services = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact(DisplayName = "Restored activity execution contexts keep their scheduling chain")]
    public async Task PreservesSchedulingChainAcrossResume()
    {
        var (suspended, resumed) = await RunAndResumeAsync(new());

        var suspendedA = GetContext(suspended, "A");
        var suspendedB = GetContext(suspended, "B");
        Assert.Equal(suspendedA.Id, suspendedB.SchedulingActivityExecutionId);
        Assert.Equal("A", suspendedB.SchedulingActivityId);

        var resumedB = Assert.Single(resumed.Journal.ActivityExecutionContexts, x => x.Id == suspendedB.Id);
        Assert.Equal(suspendedA.CallStackDepth + 1, resumedB.CallStackDepth);
        Assert.Equal(suspendedA.Id, resumedB.SchedulingActivityExecutionId);
        Assert.Equal("A", resumedB.SchedulingActivityId);

        AssertRestoredContextsMatch(suspended, resumed);
    }

    [Fact(DisplayName = "Restored root context keeps its cross-workflow scheduling chain")]
    public async Task PreservesCrossWorkflowSchedulingChainAcrossResume()
    {
        const string parentContextId = "parent-execute-workflow-context";
        const string parentInstanceId = "parent-workflow-instance";
        const int parentDepth = 4;
        var options = new RunWorkflowOptions
        {
            SchedulingActivityExecutionId = parentContextId,
            SchedulingWorkflowInstanceId = parentInstanceId,
            SchedulingCallStackDepth = parentDepth
        };

        var (suspended, resumed) = await RunAndResumeAsync(options);

        var suspendedRoot = GetRootContext(suspended);
        Assert.Equal(parentContextId, suspendedRoot.SchedulingActivityExecutionId);
        Assert.Equal(parentInstanceId, suspendedRoot.SchedulingWorkflowInstanceId);

        var resumedRoot = GetRootContext(resumed);
        Assert.Equal(suspendedRoot.Id, resumedRoot.Id);
        Assert.Equal(parentDepth + 1, resumedRoot.CallStackDepth);
        Assert.Equal(parentContextId, resumedRoot.SchedulingActivityExecutionId);
        Assert.Equal(parentInstanceId, resumedRoot.SchedulingWorkflowInstanceId);

        AssertRestoredContextsMatch(suspended, resumed);
    }

    private async Task<(RunWorkflowResult Suspended, RunWorkflowResult Resumed)> RunAndResumeAsync(RunWorkflowOptions options)
    {
        await _services.PopulateRegistriesAsync();
        var runner = _services.GetRequiredService<IWorkflowRunner>();
        var workflow = CreateWorkflow();

        var suspended = await runner.RunAsync(workflow, options);
        Assert.Equal(WorkflowSubStatus.Suspended, suspended.WorkflowState.SubStatus);
        var bookmark = Assert.Single(suspended.WorkflowState.Bookmarks, x => x.ActivityId == "B");

        var resumed = await runner.RunAsync(workflow, suspended.WorkflowState, new RunWorkflowOptions { BookmarkId = bookmark.Id });
        Assert.Equal(WorkflowSubStatus.Finished, resumed.WorkflowState.SubStatus);
        return (suspended, resumed);
    }

    private static Workflow CreateWorkflow()
    {
        var a = new WriteLine("A") { Id = "A" };
        var b = new Event("B") { Id = "B" };
        var c = new WriteLine("C") { Id = "C" };

        return new()
        {
            Root = new Flowchart
            {
                Id = "Flowchart",
                Activities = { a, b, c },
                Connections =
                {
                    new Connection(a, b),
                    new Connection(b, c)
                }
            }
        };
    }

    private static void AssertRestoredContextsMatch(RunWorkflowResult suspended, RunWorkflowResult resumed)
    {
        var restoredIds = suspended.WorkflowState.ActivityExecutionContexts.Select(x => x.Id).ToList();
        Assert.NotEmpty(restoredIds);

        foreach (var id in restoredIds)
        {
            var before = Assert.Single(suspended.Journal.ActivityExecutionContexts, x => x.Id == id);
            var after = Assert.Single(resumed.Journal.ActivityExecutionContexts, x => x.Id == id);
            Assert.Equal(before.CallStackDepth, after.CallStackDepth);
            Assert.Equal(before.SchedulingActivityExecutionId, after.SchedulingActivityExecutionId);
            Assert.Equal(before.SchedulingActivityId, after.SchedulingActivityId);
            Assert.Equal(before.SchedulingWorkflowInstanceId, after.SchedulingWorkflowInstanceId);
        }
    }

    private static ActivityExecutionContext GetContext(RunWorkflowResult result, string activityId) =>
        Assert.Single(result.Journal.ActivityExecutionContexts, x => x.Activity.Id == activityId);

    private static ActivityExecutionContext GetRootContext(RunWorkflowResult result) =>
        Assert.Single(result.Journal.ActivityExecutionContexts, x => x.ParentActivityExecutionContext == null);
}
