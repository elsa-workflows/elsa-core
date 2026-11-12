using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.Signals;
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
        var settled = await RunUntilSettledAsync(FailThenNext(new FailingActivity { Id = FailingActivity.ActivityId }));

        AssertFaultPropagated(settled);
    }

    [Fact(DisplayName = "A background activity that throws faults the workflow and records an incident")]
    public async Task BackgroundFaultIsPropagated()
    {
        var settled = await RunUntilSettledAsync(FailThenNext(new FailingActivity { Id = FailingActivity.ActivityId, RunAsynchronously = true }));

        AssertFaultPropagated(settled);
    }

    [Fact(DisplayName = "A job activity that throws faults the workflow and records an incident")]
    public async Task JobFaultIsPropagated()
    {
        var settled = await RunUntilSettledAsync(FailThenNext(new FailingJobActivity { Id = FailingActivity.ActivityId }));

        AssertFaultPropagated(settled);
    }

    [Fact(DisplayName = "A background activity that throws a cancellation exception while its job is not cancelled faults the workflow")]
    public async Task BackgroundCancellationExceptionIsPropagated()
    {
        var settled = await RunUntilSettledAsync(FailThenNext(new CancellingActivity { Id = FailingActivity.ActivityId, RunAsynchronously = true }));

        AssertFaultPropagated(settled);
    }

    [Theory(DisplayName = "A fault that an ancestor claims does not fault the workflow, and the workflow continues")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClaimedFaultLetsTheWorkflowContinue(bool runAsynchronously)
    {
        var root = new Sequence
        {
            Activities =
            {
                new ClaimingContainer
                {
                    Activities =
                    {
                        new FailingActivity { Id = FailingActivity.ActivityId, RunAsynchronously = runAsynchronously }
                    }
                },
                new WriteLine("Runs after the claimed fault") { Id = NextActivityId }
            }
        };

        var settled = await RunUntilSettledAsync(root);

        var state = settled.State;
        Assert.Equal(WorkflowStatus.Finished, state.Status);
        Assert.Equal(WorkflowSubStatus.Finished, state.SubStatus);
        Assert.Empty(state.Incidents);
        Assert.NotNull(settled.Instance);
        Assert.Equal(WorkflowSubStatus.Finished, settled.Instance.SubStatus);
        Assert.Empty(settled.Instance.WorkflowState.Incidents);
        var nextRecord = Assert.Single(settled.ActivityExecutionRecords, x => x.ActivityId == NextActivityId);
        Assert.Equal(ActivityStatus.Completed, nextRecord.Status);
        Assert.DoesNotContain(settled.StoredBookmarks, x => x.Name == BackgroundActivityStimulusBookmarkName);
    }

    private static Sequence FailThenNext(IActivity failingActivity) => new()
    {
        Activities =
        {
            failingActivity,
            new WriteLine("Should not run") { Id = NextActivityId }
        }
    };

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

        // The workflow itself is told about the fault once. A background run also publishes ActivityFaulted on its own copy
        // of the workflow, where GetIsBackgroundExecution() is true, just as it publishes ActivityExecuted there.
        Assert.Single(settled.ActivityNotifications, x => x == nameof(ActivityFaulted));
    }

    private record SettledWorkflow(
        WorkflowState State,
        WorkflowInstance? Instance,
        IReadOnlyCollection<ActivityExecutionRecord> ActivityExecutionRecords,
        IReadOnlyCollection<StoredBookmark> StoredBookmarks,
        IReadOnlyCollection<string> ActivityNotifications);

    /// <summary>
    /// Dispatches the workflow and returns the first committed state that has settled: the workflow faulted, finished,
    /// or the background activity's resume bookmark was consumed. Also reads what the commit stored for the workflow.
    /// </summary>
    private static async Task<SettledWorkflow> RunUntilSettledAsync(IActivity root)
    {
        var signal = new TaskCompletionSource<WorkflowState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stimulusBookmarkSeen = new StrongFlag(); // Shared: the handler is created per notification.
        var notifications = new System.Collections.Concurrent.ConcurrentQueue<string>();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddNotificationHandler<CommitSignal, WorkflowStateCommitted>(_ => new(signal, stimulusBookmarkSeen));
                services.AddNotificationHandler<NotificationRecorder, ActivityExecuted>(_ => new(notifications));
                services.AddNotificationHandler<NotificationRecorder, ActivityFaulted>(_ => new(notifications));
                services.AddElsa(elsa => elsa
                    .AddActivity<FailingActivity>()
                    .AddActivity<FailingJobActivity>()
                    .AddActivity<CancellingActivity>()
                    .AddActivity<ClaimingContainer>()
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
                .BuildWorkflowAsync(new TestWorkflow(builder => builder.Root = root));
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
            return new(state, instance, records.ToList(), bookmarks.ToList(), notifications.ToList());
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

    /// <summary>Always runs in the background, because it is a job, and throws there.</summary>
    [Activity("Elsa.Test", Kind = ActivityKind.Job)]
    private class FailingJobActivity : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context) => throw new InvalidOperationException(FailureMessage);
    }

    /// <summary>Throws a cancellation exception that is not caused by cancelling the job, like an HttpClient timeout.</summary>
    [Activity("Elsa.Test", Kind = ActivityKind.Task)]
    private class CancellingActivity : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context) => throw new TaskCanceledException(FailureMessage);
    }

    /// <summary>Claims every fault of its children: cancels the faulted child and completes, so the workflow continues.</summary>
    private class ClaimingContainer : TestContainer
    {
        public ClaimingContainer()
        {
            OnSignalReceived<FaultSignal>(ClaimAndCancelChildAsync);
        }

        private static async ValueTask ClaimAndCancelChildAsync(FaultSignal signal, SignalContext context)
        {
            context.StopPropagation();
            await signal.FaultedContext.CancelActivityAsync();
            await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
        }
    }

    private class StrongFlag
    {
        public bool Value { get; set; }
    }

    /// <summary>Records the activity notifications published for the failing activity.</summary>
    private class NotificationRecorder(System.Collections.Concurrent.ConcurrentQueue<string> notifications) : INotificationHandler<ActivityExecuted>, INotificationHandler<ActivityFaulted>
    {
        public Task HandleAsync(ActivityExecuted notification, CancellationToken cancellationToken) => Record(nameof(ActivityExecuted), notification.ActivityExecutionContext);

        public Task HandleAsync(ActivityFaulted notification, CancellationToken cancellationToken) => Record(nameof(ActivityFaulted), notification.ActivityExecutionContext);

        private Task Record(string name, ActivityExecutionContext context)
        {
            if (context.Activity.Id == FailingActivity.ActivityId)
            {
                notifications.Enqueue(context.GetIsBackgroundExecution() ? $"{name} (background)" : name);
            }

            return Task.CompletedTask;
        }
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
            {
                signal.TrySetResult(state);
            }

            return Task.CompletedTask;
        }
    }
}
