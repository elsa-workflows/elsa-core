using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BackgroundActivityFaults;

/// <summary>
/// An activity that throws must fault the workflow and record an incident whether it runs in the foreground or in the
/// background (<c>RunAsynchronously = true</c>). The background run executes the activity against a separate
/// <see cref="WorkflowExecutionContext"/>, so its fault has to be carried back to the workflow when it resumes.
/// </summary>
public class BackgroundActivityFaultTests
{
    private const string FailureMessage = "Background work failed.";
    private const string NextActivityId = "Next";
    private const string BackgroundActivityStimulusBookmarkName = "BackgroundActivity";

    [Fact(DisplayName = "A foreground activity that throws faults the workflow and records an incident")]
    public async Task ForegroundFaultIsPropagated()
    {
        var settled = await RunUntilSettledAsync(runAsynchronously: false);

        AssertFaultPropagated(settled);
    }

    [Fact(DisplayName = "A background activity that throws faults the workflow and records an incident")]
    public async Task BackgroundFaultIsPropagated()
    {
        var settled = await RunUntilSettledAsync(runAsynchronously: true);

        AssertFaultPropagated(settled);
    }

    private static void AssertFaultPropagated(SettledWorkflow settled)
    {
        var state = settled.State;
        var failingContext = state.ActivityExecutionContexts.FirstOrDefault(x => x.ScheduledActivityNodeId.EndsWith(FailingActivity.ActivityId));
        var summary = $"Workflow {state.Status}/{state.SubStatus}, incidents: {state.Incidents.Count}, "
                      + $"bookmarks: {state.Bookmarks.Count}, scheduled: {state.ScheduledActivities.Count}, "
                      + $"failing activity status: {failingContext?.Status.ToString() ?? "<not active>"}.";

        Assert.True(state.Incidents.Any(x => x.ActivityId == FailingActivity.ActivityId), $"No incident was recorded for the failing activity. {summary}");
        var incident = Assert.Single(state.Incidents, x => x.ActivityId == FailingActivity.ActivityId);
        Assert.Equal(FailureMessage, incident.Exception?.Message);
        Assert.True(state.SubStatus == WorkflowSubStatus.Faulted, $"Expected the workflow to be faulted. {summary}");
        Assert.Equal(WorkflowStatus.Finished, state.Status);
        Assert.DoesNotContain(state.Bookmarks, x => x.Payload is BackgroundActivityStimulus);
        Assert.DoesNotContain(state.ActivityExecutionContexts, x => x.ScheduledActivityNodeId.EndsWith(NextActivityId));

        // The stored workflow instance.
        var instance = settled.Instance;
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Faulted, instance.SubStatus);
        var storedIncident = Assert.Single(instance.WorkflowState.Incidents, x => x.ActivityId == FailingActivity.ActivityId);
        Assert.Equal(FailureMessage, storedIncident.Exception?.Message);

        // The stored activity execution records.
        var failingRecord = Assert.Single(settled.ActivityExecutionRecords, x => x.ActivityId == FailingActivity.ActivityId);
        Assert.Equal(ActivityStatus.Faulted, failingRecord.Status);
        Assert.Equal(FailureMessage, failingRecord.Exception?.Message);
        Assert.DoesNotContain(settled.ActivityExecutionRecords, x => x.ActivityId == NextActivityId);

        // The bookmark store.
        Assert.DoesNotContain(settled.StoredBookmarks, x => x.Name == BackgroundActivityStimulusBookmarkName);
    }

    private record SettledWorkflow(
        WorkflowState State,
        WorkflowInstance? Instance,
        IReadOnlyCollection<ActivityExecutionRecord> ActivityExecutionRecords,
        IReadOnlyCollection<StoredBookmark> StoredBookmarks);

    /// <summary>
    /// Dispatches the workflow and returns the first committed state that has settled: the workflow faulted, finished,
    /// or the background activity's resume bookmark was consumed. Also reads what the commit stored for the workflow.
    /// </summary>
    private static async Task<SettledWorkflow> RunUntilSettledAsync(bool runAsynchronously)
    {
        var signal = new TaskCompletionSource<WorkflowState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stimulusBookmarkSeen = new StrongFlag(); // Shared: the handler is created per notification.

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddNotificationHandler<CommitSignal, WorkflowStateCommitted>(_ => new(signal, stimulusBookmarkSeen));
                services.AddElsa(elsa => elsa
                    .AddActivity<FailingActivity>()
                    .UseWorkflowRuntime());
            })
            .Build();

        await host.StartAsync();
        try
        {
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            await serviceProvider.PopulateRegistriesAsync();
            var workflow = await serviceProvider.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder()
                .BuildWorkflowAsync(new TestWorkflow(builder => builder.Root = new Sequence
                {
                    Activities =
                    {
                        new FailingActivity { Id = FailingActivity.ActivityId, RunAsynchronously = runAsynchronously },
                        new WriteLine("Should not run") { Id = NextActivityId }
                    }
                }));
            await serviceProvider.GetRequiredService<IWorkflowRegistry>().RegisterAsync(workflow);
            var response = await serviceProvider.GetRequiredService<IWorkflowDispatcher>().DispatchAsync(new DispatchWorkflowDefinitionRequest
            {
                DefinitionVersionId = workflow.DefinitionHandle.DefinitionVersionId!,
                InstanceId = Guid.NewGuid().ToString()
            }, null);
            response.ThrowIfFailed();

            var completed = await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(completed == signal.Task, "The workflow did not settle within 5 seconds.");
            var state = await signal.Task;

            // The commit stores everything before it publishes WorkflowStateCommitted, so the stores are up to date here.
            var instance = await serviceProvider.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(state.Id);
            var records = await serviceProvider.GetRequiredService<IActivityExecutionStore>().FindManyAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = state.Id });
            var bookmarks = await serviceProvider.GetRequiredService<IBookmarkStore>().FindManyAsync(new BookmarkFilter { WorkflowInstanceId = state.Id });
            return new(state, instance, records.ToList(), bookmarks.ToList());
        }
        finally
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await host.StopAsync(stopTimeout.Token);
        }
    }

    /// <summary>Throws when executed, in the foreground or in the background.</summary>
    [Activity("Elsa.Test", Kind = ActivityKind.Task)]
    private class FailingActivity : CodeActivity
    {
        public const string ActivityId = "FailingActivity";

        protected override void Execute(ActivityExecutionContext context) => throw new InvalidOperationException(FailureMessage);
    }

    private class StrongFlag
    {
        public bool Value { get; set; }
    }

    private class CommitSignal(TaskCompletionSource<WorkflowState> signal, StrongFlag stimulusBookmarkSeen) : INotificationHandler<WorkflowStateCommitted>
    {
        public Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
        {
            var state = notification.WorkflowState;
            var hasStimulusBookmark = state.Bookmarks.Any(x => x.Payload is BackgroundActivityStimulus);
            stimulusBookmarkSeen.Value |= hasStimulusBookmark;

            var settled = state.Incidents.Any()
                          || state.SubStatus is WorkflowSubStatus.Faulted or WorkflowSubStatus.Finished
                          || (stimulusBookmarkSeen.Value && !hasStimulusBookmark);

            if (settled)
                signal.TrySetResult(state);

            return Task.CompletedTask;
        }
    }
}
