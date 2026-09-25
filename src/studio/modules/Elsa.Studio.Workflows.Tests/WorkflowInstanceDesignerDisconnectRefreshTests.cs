using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Pins that <see cref="WorkflowInstanceDesigner"/>'s periodic activity-state refresh timer stops
/// quietly instead of crashing the process when the Blazor circuit it belongs to disconnects
/// (see https://github.com/elsa-workflows/elsa-studio/issues/743).
/// </summary>
public sealed class WorkflowInstanceDesignerDisconnectRefreshTests : BunitContext, IAsyncLifetime
{
    public WorkflowInstanceDesignerDisconnectRefreshTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ILocalizer>(new TestLocalizer());
        Services.AddSingleton<IActivityRegistry>(new ActivityRegistryStub());
        Services.AddSingleton<IRemoteFeatureProvider>(new RemoteFeatureProviderStub());
        Services.AddSingleton(DispatchProxy.Create<IDiagramDesignerService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IDomAccessor, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IActivityVisitor, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceObserverFactory, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, ThrowingProxy>());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task RefreshTickAfterDisposalDoesNotThrowOrCallActivityExecutionService()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        var exception = await Record.ExceptionAsync(() => InvokeRefreshTimerTickAsync(cut.Instance, "exec-1"));

        Assert.Null(exception);
        Assert.Equal(0, activityExecutionService.ListSummariesCallCount);
    }

    public static IEnumerable<object[]> CircuitGoneExceptions()
    {
        yield return new object[] { new JSDisconnectedException("The circuit has disconnected.") };
        yield return new object[] { new ObjectDisposedException("ActivityExecutionService") };
        yield return new object[] { new OperationCanceledException("The operation was canceled.") };
    }

    [Theory]
    [MemberData(nameof(CircuitGoneExceptions))]
    public async Task RefreshTickStopsPeriodicRefreshWhenCircuitIsGone(Exception circuitGoneException)
    {
        var activityExecutionService = new RecordingActivityExecutionService(circuitGoneException);
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");
        using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetRefreshTimer(cut.Instance, timer);

        try
        {
            var exception = await Record.ExceptionAsync(() => InvokeRefreshTimerTickAsync(cut.Instance, "exec-1"));

            Assert.Null(exception);
            Assert.Equal(1, activityExecutionService.ListSummariesCallCount);

            // The periodic refresh timer has been stopped and disposed in response to the circuit-gone
            // exception, so the real Timer can no longer produce a subsequent tick.
            Assert.Null(GetRefreshTimer(cut.Instance));
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    [Fact]
    public async Task ElapsedTickAfterDisposalDoesNothing()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        var exception = await Record.ExceptionAsync(() => cut.Instance.ElapsedTimerTickAsync());

        Assert.Null(exception);
        Assert.Equal(0, cut.Instance.NotifyStateChangedCallCount);
    }

    [Fact]
    public async Task ElapsedTickBeforeDisposalNotifiesStateChanged()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        await cut.Instance.ElapsedTimerTickAsync();

        Assert.Equal(1, cut.Instance.NotifyStateChangedCallCount);
    }

    [Theory]
    [MemberData(nameof(CircuitGoneExceptions))]
    public async Task ElapsedTickStopsElapsedTimerWhenCircuitIsGone(Exception circuitGoneException)
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        cut.Instance.ThrowOnRender = circuitGoneException;
        using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetElapsedTimer(cut.Instance, timer);

        try
        {
            var exception = await Record.ExceptionAsync(() => cut.Instance.ElapsedTimerTickAsync());

            Assert.Null(exception);

            // The elapsed timer has been stopped and disposed in response to the circuit-gone exception,
            // so the real Timer can no longer produce a subsequent tick.
            Assert.Null(GetElapsedTimer(cut.Instance));
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    [Fact]
    public async Task RefreshTimerTickRearmToleratesConcurrentlyDisposedTimer()
    {
        var runningRecord = new ActivityExecutionRecord
        {
            Id = "exec-1",
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityType = "Test",
            Status = ActivityStatus.Running
        };
        var summary = new ActivityExecutionRecordSummary
        {
            Id = "exec-1",
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityType = "Test",
            Status = ActivityStatus.Running
        };
        var activityExecutionService = new RecordingActivityExecutionService(summariesToReturn: [summary], recordToReturn: runningRecord);
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        // Simulate the timer being disposed concurrently (e.g. by DisposeAsync racing this tick) right
        // before the tick tries to rearm it.
        var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetRefreshTimer(cut.Instance, timer);
        timer.Dispose();

        var exception = await Record.ExceptionAsync(() => InvokeRefreshTimerTickAsync(cut.Instance, "exec-1"));

        Assert.Null(exception);
    }

    /// <summary>
    /// Pins that the refresh timer is detached from <c>_refreshTimer</c> atomically, before the
    /// (potentially slow) <see cref="Timer.DisposeAsync"/> call completes. To exercise that, the
    /// refresh timer's callback is kept running (blocked on <paramref name="release"/> below) while
    /// the first disposal is in flight, so a second, concurrent disposal genuinely overlaps with it
    /// instead of running after the first has already finished.
    /// </summary>
    [Fact]
    public async Task ConcurrentDisposeCallsDetachTimersAtomicallyWithoutThrowing()
    {
        var timeout = TimeSpan.FromSeconds(5);
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var refreshTimer = new Timer(_ =>
        {
            started.Set();
            release.Wait(timeout);
        }, null, Timeout.Infinite, Timeout.Infinite);
        using var elapsedTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);

        SetRefreshTimer(cut.Instance, refreshTimer);
        SetElapsedTimer(cut.Instance, elapsedTimer);

        // Fire the refresh timer's callback immediately and wait for it to actually start running.
        refreshTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        Assert.True(started.Wait(timeout), "The refresh timer callback did not start in time.");

        var disposable = (IAsyncDisposable)cut.Instance;

        // System.Threading.Timer.DisposeAsync only completes once any in-flight callback finishes, so
        // this first disposal stays pending while the callback above is blocked on `release`.
        var firstDisposeTask = disposable.DisposeAsync().AsTask();

        // The atomic Interlocked.Exchange detach in StopRefreshActivityStatePeriodically happens
        // before the timer is awaited, so the field is already cleared while the first disposal is
        // still pending. Against the previous check/await/clear implementation, this assertion would
        // still hold, but the second call below would then observe a non-null field and race to
        // dispose/clear it itself instead of being a no-op.
        Assert.Null(GetRefreshTimer(cut.Instance));

        var secondDisposeException = await Record.ExceptionAsync(() => disposable.DisposeAsync().AsTask());

        Assert.Null(secondDisposeException);
        Assert.False(firstDisposeTask.IsCompleted, "The first disposal should still be pending on the blocked callback.");

        release.Set();

        var completedTask = await Task.WhenAny(firstDisposeTask, Task.Delay(timeout));
        Assert.Same(firstDisposeTask, completedTask);

        var firstDisposeException = await Record.ExceptionAsync(() => firstDisposeTask);

        Assert.Null(firstDisposeException);
        Assert.Null(GetRefreshTimer(cut.Instance));
        Assert.Null(GetElapsedTimer(cut.Instance));
    }

    /// <summary>
    /// Pins that stopping the refresh timer from within its own tick (the path used by
    /// <see cref="WorkflowInstanceDesigner.RefreshTimerTickAsync"/>'s terminal-state branch and by
    /// <see cref="RunTimerTickAsync"/>'s stop delegate) does not wait for an in-flight callback to
    /// return (see https://github.com/elsa-workflows/elsa-studio/issues/743).
    /// <see cref="Timer.DisposeAsync"/> only completes once any callback currently executing on the
    /// timer has returned, regardless of which thread calls it, so awaiting it from the callback that
    /// is itself executing would deadlock. This test keeps the timer's own callback blocked (simulating
    /// it still being "in flight") and invokes the private, non-waiting stop method directly -
    /// deliberately bypassing the render pipeline (<c>InvokeAsync</c>/<c>StateHasChanged</c>) so the
    /// assertion is not confounded by ThreadPool contention between the blocked callback and the
    /// renderer's dispatcher. Against an implementation that used the draining, <c>DisposeAsync</c>-based
    /// stop from this path instead, the call below would block until the callback released.
    /// </summary>
    [Fact]
    public void StoppingRefreshTimerFromTickPathDoesNotWaitForInFlightCallback()
    {
        var startTimeout = TimeSpan.FromSeconds(5);
        var assertionBound = TimeSpan.FromMilliseconds(500);
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var refreshTimer = new Timer(_ =>
        {
            started.Set();

            // Block for longer than the assertion bound below (but still bounded, so this thread is
            // not tied up indefinitely if the assertion below fails), keeping the callback genuinely
            // "in flight" for the whole window the assertion is checking.
            release.Wait(TimeSpan.FromSeconds(10));
        }, null, Timeout.Infinite, Timeout.Infinite);

        SetRefreshTimer(cut.Instance, refreshTimer);

        // Fire the timer's own callback and wait for it to actually start running, so a genuine
        // callback is in flight on the timer while the stop call below tries to stop it.
        refreshTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        Assert.True(started.Wait(startTimeout), "The refresh timer callback did not start in time.");

        try
        {
            var stopMethod = typeof(WorkflowInstanceDesigner).GetMethod("StopRefreshTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var stopwatch = Stopwatch.StartNew();

            stopMethod.Invoke(cut.Instance, null);

            Assert.True(stopwatch.Elapsed < assertionBound, $"Stopping the timer from the tick path took {stopwatch.Elapsed}, which suggests it waited for the blocked callback.");
            Assert.Null(GetRefreshTimer(cut.Instance));
        }
        finally
        {
            release.Set();
        }
    }

    [Fact]
    public async Task StartElapsedTimerAfterDisposalLeavesTimerFieldNull()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        cut.Instance.StartElapsedTimer();

        Assert.Null(GetElapsedTimer(cut.Instance));
    }

    [Fact]
    public async Task StartElapsedTimerOnLiveComponentInstallsTimerOnce()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        try
        {
            cut.Instance.StartElapsedTimer();
            var firstTimer = GetElapsedTimer(cut.Instance);
            Assert.NotNull(firstTimer);

            cut.Instance.StartElapsedTimer();
            var secondTimer = GetElapsedTimer(cut.Instance);

            Assert.Same(firstTimer, secondTimer);
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    /// <summary>
    /// Pins that <see cref="WorkflowInstanceDesigner.StartElapsedTimer"/> arms the timer it publishes
    /// (not just creates it disabled), by waiting for a real tick to reach
    /// <see cref="TestWorkflowInstanceDesigner.NotifyStateChangedAsync"/>.
    /// </summary>
    [Fact]
    public async Task StartElapsedTimerOnLiveComponentArmsTimerAndTicks()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        try
        {
            cut.Instance.StartElapsedTimer();

            var ticked = await WaitUntilAsync(() => cut.Instance.NotifyStateChangedCallCount > 0, TimeSpan.FromSeconds(5));

            Assert.True(ticked, "The elapsed timer did not tick within the bounded wait.");
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    /// <summary>
    /// Pins that <see cref="WorkflowInstanceDesigner.RefreshActivityStatePeriodically"/> arms the timer
    /// it publishes (not just creates it disabled), by waiting for a real tick to reach
    /// <see cref="RecordingActivityExecutionService.ListSummariesAsync"/>.
    /// </summary>
    [Fact]
    public async Task RefreshActivityStatePeriodicallyOnLiveComponentArmsTimerAndTicks()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        try
        {
            cut.Instance.RefreshActivityStatePeriodically("exec-1");

            var ticked = await WaitUntilAsync(() => activityExecutionService.ListSummariesCallCount > 0, TimeSpan.FromSeconds(5));

            Assert.True(ticked, "The refresh timer did not tick within the bounded wait.");
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;

            await Task.Delay(20);
        }

        return condition();
    }

    [Fact]
    public async Task DisposeAsyncStopsRefreshTimerEvenWhenObserverDisposalThrows()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        var observerException = new InvalidOperationException("Observer disposal failed.");
        SetWorkflowInstanceObserver(cut.Instance, new ThrowingWorkflowInstanceObserver(observerException));

        using var refreshTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetRefreshTimer(cut.Instance, refreshTimer);

        var exception = await Record.ExceptionAsync(() => ((IAsyncDisposable)cut.Instance).DisposeAsync().AsTask());

        Assert.Same(observerException, exception);
        Assert.Null(GetRefreshTimer(cut.Instance));
    }

    [Fact]
    public async Task RefreshActivityStatePeriodicallyAfterDisposalLeavesTimerFieldNull()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        cut.Instance.RefreshActivityStatePeriodically("exec-1");

        Assert.Null(GetRefreshTimer(cut.Instance));
    }

    [Fact]
    public async Task RefreshActivityStatePeriodicallyOnLiveComponentInstallsTimerOnce()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        try
        {
            cut.Instance.RefreshActivityStatePeriodically("exec-1");
            var firstTimer = GetRefreshTimer(cut.Instance);
            Assert.NotNull(firstTimer);

            cut.Instance.RefreshActivityStatePeriodically("exec-1");
            var secondTimer = GetRefreshTimer(cut.Instance);

            Assert.NotNull(secondTimer);
            Assert.NotSame(firstTimer, secondTimer);
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    /// <summary>
    /// Pins that a <see cref="WorkflowInstanceObserver"/> created by a factory call that was still in
    /// flight when <c>DisposeAsync</c> ran is disposed immediately instead of being published and
    /// subscribed on the torn-down component (see
    /// https://github.com/elsa-workflows/elsa-studio/issues/743).
    /// </summary>
    [Fact]
    public async Task CreateObserverAsyncDisposesObserverCreatedAfterDisposal()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var factory = new GatedWorkflowInstanceObserverFactory();
        var cut = RenderDesigner(activityExecutionService, factory);
        SetDesigner(cut.Instance, new JsonObject());

        var createTask = cut.Instance.CreateObserverAsync();
        Assert.True(factory.CreateAsyncEntered.Wait(TimeSpan.FromSeconds(5)), "The factory was not called in time.");

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        var observer = new CountingWorkflowInstanceObserver();
        factory.Release(observer);

        await createTask;

        Assert.Null(GetWorkflowInstanceObserver(cut.Instance));
        Assert.Equal(1, observer.DisposeCallCount);
        Assert.Equal(0, observer.SubscribeCount);
    }

    /// <summary>
    /// Keeps the live-circuit path exercised: when the component is not disposed, a created observer
    /// is still published to <see cref="WorkflowInstanceObserver"/> and subscribed to.
    /// </summary>
    [Fact]
    public async Task CreateObserverAsyncOnLiveComponentPublishesAndSubscribesObserver()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var factory = new GatedWorkflowInstanceObserverFactory();
        var cut = RenderDesigner(activityExecutionService, factory);
        SetDesigner(cut.Instance, new JsonObject());

        var observer = new CountingWorkflowInstanceObserver();
        var createTask = cut.Instance.CreateObserverAsync();
        Assert.True(factory.CreateAsyncEntered.Wait(TimeSpan.FromSeconds(5)), "The factory was not called in time.");
        factory.Release(observer);

        await createTask;

        try
        {
            Assert.Same(observer, GetWorkflowInstanceObserver(cut.Instance));
            Assert.Equal(1, observer.SubscribeCount);
            Assert.Equal(0, observer.DisposeCallCount);
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    private IRenderedComponent<TestWorkflowInstanceDesigner> RenderDesigner(
        IActivityExecutionService activityExecutionService,
        IWorkflowInstanceObserverFactory? observerFactory = null)
    {
        Services.AddSingleton(activityExecutionService);

        if (observerFactory != null)
            Services.AddSingleton(observerFactory);

        var workflowInstance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "definition-1",
            Status = WorkflowStatus.Finished
        };

        return Render<TestWorkflowInstanceDesigner>(parameters => parameters
            .Add(x => x.WorkflowInstance, workflowInstance));
    }

    private static void SetLastActivityExecution(WorkflowInstanceDesigner instance, string activityNodeId)
    {
        var property = typeof(WorkflowInstanceDesigner).GetProperty("LastActivityExecution", BindingFlags.Instance | BindingFlags.NonPublic)!;
        property.SetValue(instance, new ActivityExecutionRecord
        {
            Id = "exec-1",
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = activityNodeId,
            ActivityType = "Test",
            Status = ActivityStatus.Running
        });
    }

    private const string RefreshTimerFieldName = "_refreshTimer";
    private const string ElapsedTimerFieldName = "_elapsedTimer";

    private static void SetRefreshTimer(WorkflowInstanceDesigner instance, Timer timer) =>
        SetTimer(instance, RefreshTimerFieldName, timer);

    private static Timer? GetRefreshTimer(WorkflowInstanceDesigner instance) =>
        GetTimer(instance, RefreshTimerFieldName);

    private static Task InvokeRefreshTimerTickAsync(WorkflowInstanceDesigner instance, string activityExecutionRecordId) =>
        instance.RefreshTimerTickAsync(activityExecutionRecordId);

    private static void SetElapsedTimer(WorkflowInstanceDesigner instance, Timer timer) =>
        SetTimer(instance, ElapsedTimerFieldName, timer);

    private static Timer? GetElapsedTimer(WorkflowInstanceDesigner instance) =>
        GetTimer(instance, ElapsedTimerFieldName);

    private static Timer? GetTimer(WorkflowInstanceDesigner instance, string fieldName) =>
        (Timer?)GetTimerField(fieldName).GetValue(instance);

    private static void SetTimer(WorkflowInstanceDesigner instance, string fieldName, Timer? value) =>
        GetTimerField(fieldName).SetValue(instance, value);

    private static FieldInfo GetTimerField(string fieldName) =>
        typeof(WorkflowInstanceDesigner).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void SetWorkflowInstanceObserver(WorkflowInstanceDesigner instance, IWorkflowInstanceObserver observer) =>
        GetWorkflowInstanceObserverProperty().SetValue(instance, observer);

    private static IWorkflowInstanceObserver? GetWorkflowInstanceObserver(WorkflowInstanceDesigner instance) =>
        (IWorkflowInstanceObserver?)GetWorkflowInstanceObserverProperty().GetValue(instance);

    private static PropertyInfo GetWorkflowInstanceObserverProperty() =>
        typeof(WorkflowInstanceDesigner).GetProperty("WorkflowInstanceObserver", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// Attaches a bare <see cref="DiagramDesignerWrapper"/> (not rendered through bUnit, so its own
    /// injected dependencies are never touched) to <c>_designer</c>, with its <c>Activity</c> parameter
    /// set via reflection to avoid setting a component parameter outside of its render pipeline.
    /// </summary>
    private static void SetDesigner(WorkflowInstanceDesigner instance, JsonObject activity)
    {
        var designer = new DiagramDesignerWrapper();
        var activityProperty = typeof(DiagramDesignerWrapper).GetProperty(nameof(DiagramDesignerWrapper.Activity))!;
        activityProperty.SetValue(designer, activity);

        var field = typeof(WorkflowInstanceDesigner).GetField("_designer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, designer);
    }

    /// <summary>
    /// An <see cref="IWorkflowInstanceObserverFactory"/> whose <see cref="CreateAsync(WorkflowInstanceObserverContext)"/>
    /// blocks until <see cref="Release"/> is called, used to pin the window during which
    /// <c>WorkflowInstanceDesigner.CreateObserverAsync</c> is awaiting the factory when disposal runs.
    /// </summary>
    private sealed class GatedWorkflowInstanceObserverFactory : IWorkflowInstanceObserverFactory
    {
        private readonly TaskCompletionSource<IWorkflowInstanceObserver> _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// Signaled once <see cref="CreateAsync(WorkflowInstanceObserverContext)"/> has been called.
        public ManualResetEventSlim CreateAsyncEntered { get; } = new(false);

        public Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId) => throw new NotSupportedException();

        public Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context)
        {
            CreateAsyncEntered.Set();
            return _gate.Task;
        }

        /// Unblocks the pending <see cref="CreateAsync(WorkflowInstanceObserverContext)"/> call with the given observer.
        public void Release(IWorkflowInstanceObserver observer) => _gate.SetResult(observer);
    }

    /// <summary>
    /// An <see cref="IWorkflowInstanceObserver"/> that counts subscriptions and disposals, used to pin
    /// that an observer created after disposal is disposed without being subscribed, while an observer
    /// created on a live component is both subscribed and left undisposed.
    /// </summary>
    private sealed class CountingWorkflowInstanceObserver : IWorkflowInstanceObserver
    {
        public int DisposeCallCount { get; private set; }
        public int SubscribeCount { get; private set; }
        public int UnsubscribeCount { get; private set; }

        public event Func<Elsa.Api.Client.RealTime.Messages.WorkflowExecutionLogUpdatedMessage, Task>? WorkflowJournalUpdated
        {
            add { }
            remove { }
        }

        public event Func<Elsa.Api.Client.RealTime.Messages.ActivityExecutionLogUpdatedMessage, Task>? ActivityExecutionLogUpdated
        {
            add => SubscribeCount++;
            remove => UnsubscribeCount++;
        }

        public event Func<Elsa.Api.Client.RealTime.Messages.WorkflowInstanceUpdatedMessage, Task>? WorkflowInstanceUpdated
        {
            add { }
            remove { }
        }

        public ValueTask DisposeAsync()
        {
            DisposeCallCount++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// An <see cref="IWorkflowInstanceObserver"/> whose <see cref="DisposeAsync"/> throws, used to pin
    /// that the periodic refresh timer is still stopped when observer disposal faults.
    /// </summary>
    private sealed class ThrowingWorkflowInstanceObserver(Exception exceptionToThrow) : IWorkflowInstanceObserver
    {
        public event Func<Elsa.Api.Client.RealTime.Messages.WorkflowExecutionLogUpdatedMessage, Task>? WorkflowJournalUpdated
        {
            add { }
            remove { }
        }

        public event Func<Elsa.Api.Client.RealTime.Messages.ActivityExecutionLogUpdatedMessage, Task>? ActivityExecutionLogUpdated
        {
            add { }
            remove { }
        }

        public event Func<Elsa.Api.Client.RealTime.Messages.WorkflowInstanceUpdatedMessage, Task>? WorkflowInstanceUpdated
        {
            add { }
            remove { }
        }

        public ValueTask DisposeAsync() => throw exceptionToThrow;
    }

    /// <summary>
    /// A <see cref="WorkflowInstanceDesigner"/> whose state-changed notification can be made to throw
    /// on demand. bUnit's test renderer does not propagate exceptions from the JS-interop-driven
    /// render pipeline back through <c>InvokeAsync(StateHasChanged)</c> the way a real Blazor circuit
    /// does, so the internal <see cref="WorkflowInstanceDesigner.NotifyStateChangedAsync"/> seam is
    /// overridden here to simulate the circuit-gone exception that a real disconnect would surface
    /// from that call.
    /// </summary>
    private sealed class TestWorkflowInstanceDesigner : WorkflowInstanceDesigner
    {
        public Exception? ThrowOnRender { get; set; }
        public int NotifyStateChangedCallCount { get; private set; }

        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }

        internal override Task NotifyStateChangedAsync()
        {
            NotifyStateChangedCallCount++;
            return ThrowOnRender != null ? Task.FromException(ThrowOnRender) : base.NotifyStateChangedAsync();
        }
    }

    /// <summary>
    /// An <see cref="IActivityExecutionService"/> that counts calls to <see cref="ListSummariesAsync"/>
    /// and, when constructed with an exception, throws it from that call to simulate a circuit
    /// disconnecting mid-refresh.
    /// </summary>
    private sealed class RecordingActivityExecutionService(
        Exception? exceptionToThrow = null,
        IEnumerable<ActivityExecutionRecordSummary>? summariesToReturn = null,
        ActivityExecutionRecord? recordToReturn = null) : IActivityExecutionService
    {
        public int ListSummariesCallCount { get; private set; }

        public Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
        {
            ListSummariesCallCount++;

            if (exceptionToThrow != null)
                throw exceptionToThrow;

            return Task.FromResult(summariesToReturn ?? []);
        }

        public Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            recordToReturn != null ? Task.FromResult<ActivityExecutionRecord?>(recordToReturn) : throw new NotSupportedException();

        public Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ActivityRegistryStub : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor> List() => throw new NotSupportedException();
        public Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor? Find(string activityType, int? version = null) => throw new NotSupportedException();
        public IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor> FindAll(string activityType) => throw new NotSupportedException();
        public void MarkStale() => throw new NotSupportedException();
    }

    private sealed class RemoteFeatureProviderStub : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    /// <summary>
    /// A <see cref="DispatchProxy"/> that throws for every call, used for services this component
    /// depends on but that these tests never exercise.
    /// </summary>
    private class ThrowingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call to {targetMethod!.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}
