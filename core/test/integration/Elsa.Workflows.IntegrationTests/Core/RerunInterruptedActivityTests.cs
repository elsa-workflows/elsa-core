using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Core;

/// <summary>
/// Restoring the state saved at a commit and running it without a bookmark or activity handle, which is what the
/// runtime does when it restarts an interrupted instance (#8467).
/// Flowchart: Start -> Invoice, and optionally Start -> Docs (waits on a bookmark), so Docs is still scheduled when Invoice runs.
/// Invoice either completes at once or waits on a bookmark; a waiting activity's context stays in the saved state.
/// </summary>
public class RerunInterruptedActivityTests(ITestOutputHelper testOutputHelper)
{
    [Theory(DisplayName = "Restoring the state saved at Invoice's commit re-runs Invoice only if it was interrupted while executing")]
    [InlineData("activity-executing", false, false)]
    [InlineData("activity-executing", false, true)]
    [InlineData("activity-executing", true, false)]
    [InlineData("activity-executing", true, true)]
    [InlineData("workflow-activity-executing", false, false)]
    [InlineData("workflow-activity-executing", false, true)]
    [InlineData("workflow-activity-executing", true, false)]
    [InlineData("workflow-activity-executing", true, true)]
    [InlineData("activity-executed", false, false)]
    [InlineData("activity-executed", false, true)]
    [InlineData("activity-executed", true, false)]
    [InlineData("activity-executed", true, true)]
    [InlineData("workflow-activity-executed", false, false)]
    [InlineData("workflow-activity-executed", false, true)]
    [InlineData("workflow-activity-executed", true, false)]
    [InlineData("workflow-activity-executed", true, true)]
    public async Task RestoredRunReRunsOnlyInterruptedActivity(string commitStrategy, bool invoiceBlocks, bool withSibling)
    {
        // Arrange
        var start = new Step();
        var invoice = new Step { BookmarkName = invoiceBlocks ? "Invoice" : null };
        var docs = new Step { BookmarkName = "Docs" };
        var flowchart = new Flowchart { Start = start, Activities = { start, invoice }, Connections = { new Connection(start, invoice) } };
        if (withSibling)
        {
            flowchart.Activities.Add(docs);
            flowchart.Connections.Add(new Connection(start, docs));
        }
        var workflow = new Workflow { Root = flowchart };
        var snapshot = new SnapshotAtCommit(context => context.ActivityExecutionContexts.Any(x => x.Activity == invoice));
        var runner = BuildServices(commitStrategy, snapshot).GetRequiredService<IWorkflowRunner>();
        await runner.RunAsync(workflow);
        var savedState = snapshot.Restore();
        start.Runs = invoice.Runs = docs.Runs = 0;
        var expectedInvoiceRuns = commitStrategy.EndsWith("executing") ? 1 : 0;

        // Act
        var state = (await runner.RunAsync(workflow, savedState)).WorkflowState;

        // Assert
        Assert.Equal(expectedInvoiceRuns, invoice.Runs);
        Assert.Equal(0, start.Runs);
        Assert.DoesNotContain(state.ActivityExecutionContexts, x => x.IsExecuting);
        Assert.Equal(invoiceBlocks ? 1 : 0, state.Bookmarks.Count(x => x.Name == "Invoice"));
        Assert.Equal(withSibling ? 1 : 0, state.Bookmarks.Count(x => x.Name == "Docs"));
        var expectedStatus = invoiceBlocks || withSibling ? (WorkflowStatus.Running, WorkflowSubStatus.Suspended) : (WorkflowStatus.Finished, WorkflowSubStatus.Finished);
        Assert.Equal(expectedStatus, (state.Status, state.SubStatus));
    }

    [Fact(DisplayName = "An interrupted activity re-runs before its flowchart completes, even when a sibling branch completes first")]
    public async Task FlowchartWaitsForReRunActivity()
    {
        // Arrange
        var start = new Step();
        var invoice = new Step();
        var sibling = new Step();
        var flowchart = new Flowchart { Start = start, Activities = { start, invoice, sibling }, Connections = { new Connection(start, invoice), new Connection(start, sibling) } };
        var workflow = new Workflow { Root = flowchart };
        var snapshot = new SnapshotAtCommit(context => context.ActivityExecutionContexts.Any(x => x.Activity == invoice));
        var runner = BuildServices("activity-executing", snapshot).GetRequiredService<IWorkflowRunner>();
        await runner.RunAsync(workflow);
        var savedState = snapshot.Restore();
        start.Runs = invoice.Runs = sibling.Runs = 0;

        // Act
        var state = (await runner.RunAsync(workflow, savedState)).WorkflowState;

        // Assert
        Assert.Equal(1, invoice.Runs);
        Assert.Equal(1, sibling.Runs);
        Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Finished), (state.Status, state.SubStatus));
    }

    [Fact(DisplayName = "Running a started instance with no scheduled work, no bookmark and no interrupted activity does nothing: it doesn't start again from the root and stays Running/Suspended")]
    public async Task StartedInstanceWithNothingToRunDoesNothing()
    {
        // Arrange: suspend on Docs, then drop its bookmark, so the saved state has activity contexts and nothing that can run.
        var start = new Step();
        var docs = new Step { BookmarkName = "Docs" };
        var workflow = new Workflow { Root = new Flowchart { Start = start, Activities = { start, docs }, Connections = { new Connection(start, docs) } } };
        var services = BuildServices(commitStrategy: null, new SnapshotAtCommit(_ => false));
        var runner = services.GetRequiredService<IWorkflowRunner>();
        var serializer = services.GetRequiredService<IWorkflowStateSerializer>();
        var suspended = (await runner.RunAsync(workflow)).WorkflowState;
        var savedState = serializer.Deserialize(serializer.Serialize(suspended));
        savedState.Bookmarks.Clear();
        Assert.NotEmpty(savedState.ActivityExecutionContexts);
        Assert.Empty(savedState.ScheduledActivities);
        Assert.DoesNotContain(savedState.ActivityExecutionContexts, x => x.IsExecuting);
        start.Runs = docs.Runs = 0;

        // Act
        var state = (await runner.RunAsync(workflow, savedState)).WorkflowState;

        // Assert
        Assert.Equal(0, start.Runs);
        Assert.Equal(0, docs.Runs);
        Assert.Empty(state.Bookmarks);
        Assert.Equal((WorkflowStatus.Running, WorkflowSubStatus.Suspended), (state.Status, state.SubStatus));
    }

    private IServiceProvider BuildServices(string? commitStrategy, SnapshotAtCommit snapshot) => new TestApplicationBuilder(testOutputHelper)
        .ConfigureElsa(elsa => elsa.UseWorkflows(workflows =>
        {
            workflows.CommitStateHandler = _ => snapshot;
            switch (commitStrategy)
            {
                case "activity-executing": workflows.WithDefaultActivityCommitStrategy(new ExecutingActivityStrategy()); break;
                case "activity-executed": workflows.WithDefaultActivityCommitStrategy(new ExecutedActivityStrategy()); break;
                case "workflow-activity-executing": workflows.WithDefaultWorkflowCommitStrategy(new ActivityExecutingWorkflowStrategy()); break;
                case "workflow-activity-executed": workflows.WithDefaultWorkflowCommitStrategy(new ActivityExecutedWorkflowStrategy()); break;
            }
        }))
        .Build();

    /// <summary>Counts its runs, and waits on a bookmark when it has a bookmark name.</summary>
    private class Step : Activity
    {
        public int Runs;
        public string? BookmarkName { get; init; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            Runs++;
            if (BookmarkName == null)
                await context.CompleteActivityAsync();
            else
                context.CreateBookmark(new CreateBookmarkArgs { BookmarkName = BookmarkName, Callback = OnResumeAsync });
        }

        private static ValueTask OnResumeAsync(ActivityExecutionContext context) => context.CompleteActivityAsync();
    }

    /// <summary>
    /// Keeps a copy of the state of the first commit that matches, as a database would hold it if the process died right after
    /// that commit. Later commits are ignored.
    /// </summary>
    private class SnapshotAtCommit(Func<WorkflowExecutionContext, bool> predicate) : ICommitStateHandler
    {
        private string? _json;
        private IWorkflowStateSerializer? _serializer;

        public WorkflowState Restore() => _serializer!.Deserialize(_json ?? throw new InvalidOperationException("No matching commit happened."));

        public Task CommitAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default) =>
            CommitAsync(context, context.GetRequiredService<IWorkflowStateExtractor>().Extract(context), cancellationToken);

        public Task CommitAsync(WorkflowExecutionContext context, WorkflowState state, CancellationToken cancellationToken = default)
        {
            if (_json == null && predicate(context))
            {
                _serializer = context.GetRequiredService<IWorkflowStateSerializer>();
                _json = _serializer.Serialize(state);
            }

            return Task.CompletedTask;
        }
    }
}
