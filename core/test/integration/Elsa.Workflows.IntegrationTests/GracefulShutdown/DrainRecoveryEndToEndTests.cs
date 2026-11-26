using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// End-to-end drain recovery using the production <see cref="IWorkflowRestarter"/> (not a recording fake).
/// A recoverable instance runs a first step, then blocks on a gated activity (still Running). Drain marks
/// that live row Interrupted. After the real restarter resumes it, the remaining work completes and the
/// first step is not re-run. A separate instance whose activity honours its cancellation token is stopped by the
/// drain's force-cancel, also left Running/Interrupted, and recovered (#8489).
/// </summary>
public class DrainRecoveryEndToEndTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IDrainOrchestrator _orchestrator;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly CooperativeWaitGate _cooperativeGate = new();

    public DrainRecoveryEndToEndTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<DrainRecoveryEndToEndTests>()
            .AddWorkflow<RecoverableResumeWorkflow>()
            .AddWorkflow<CooperativeWaitWorkflow>()
            .ConfigureServices(services => services.AddSingleton(_cooperativeGate))
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(o =>
                {
                    o.DrainDeadline = TimeSpan.FromMilliseconds(50);
                    o.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                })))
            .Build();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
        _orchestrator = _services.GetRequiredService<IDrainOrchestrator>();
    }

    [Fact(DisplayName = "Real restarter resumes a drain-marked Running/Interrupted instance without re-running completed work, and also recovers a drain-cancelled instance whose activity honoured its token")]
    public async Task RealRestarterResumesInterruptedRunningAndDrainCancelledInstances()
    {
        await _services.PopulateRegistriesAsync();
        ResumeGate.Current = new ResumeGate();

        var recoverableClient = await _workflowRuntime.CreateClientAsync();
        var created = await recoverableClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(RecoverableResumeWorkflow), VersionOptions.Published)
        });
        Assert.False(created.CannotStart);

        var recoverableRun = Task.Run(() => recoverableClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));
        BackgroundCommandSenderHostedService? commandProcessor = null;
        Task? cooperativeRun = null;
        try
        {
            await ResumeGate.Current.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var arrangeScope = _services.CreateScope();
            var instanceStore = arrangeScope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            await WaitUntilAsync(async () =>
            {
                var current = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = recoverableClient.WorkflowInstanceId });
                return current is { Status: WorkflowStatus.Running }
                       && current.WorkflowState.ActivityExecutionContexts.Any(context => context.IsExecuting)
                       && _capturingTextWriter.Lines.Contains("first");
            }, TimeSpan.FromSeconds(5));

            Assert.Equal(["first"], _capturingTextWriter.Lines.ToList());

            var cooperativeClient = await _workflowRuntime.CreateClientAsync();
            Assert.False((await cooperativeClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CooperativeWaitWorkflow), VersionOptions.Published)
            })).CannotStart);
            cooperativeRun = Task.Run(() => cooperativeClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));
            await _cooperativeGate.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var outcome = await _orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

            // The force-cancel reaches the activity through its cancellation token (#8489), so the runner stops without committing.
            Assert.True(_cooperativeGate.Stopped.Task.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cooperativeRun.WaitAsync(TimeSpan.FromSeconds(5)));

            Assert.Equal(DrainResult.DeadlineExceeded, outcome.OverallResult);
            Assert.Equal(2, outcome.ExecutionCyclesForceCancelledCount);
            Assert.Contains(recoverableClient.WorkflowInstanceId, outcome.ForceCancelledInstanceIds);
            Assert.Contains(cooperativeClient.WorkflowInstanceId, outcome.ForceCancelledInstanceIds);

            var interrupted = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = recoverableClient.WorkflowInstanceId });
            Assert.NotNull(interrupted);
            Assert.Equal(WorkflowStatus.Running, interrupted.Status);
            Assert.Equal(WorkflowSubStatus.Interrupted, interrupted.SubStatus);
            Assert.False(interrupted.IsExecuting);
            Assert.Equal(["first"], _capturingTextWriter.Lines.ToList());

            // Before #8489 the drain-cancelled instance ended Finished/Cancelled and was never recovered.
            var drainCancelled = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = cooperativeClient.WorkflowInstanceId });
            Assert.NotNull(drainCancelled);
            Assert.Equal(WorkflowStatus.Running, drainCancelled.Status);
            Assert.Equal(WorkflowSubStatus.Interrupted, drainCancelled.SubStatus);

            commandProcessor = _services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
            await commandProcessor.StartAsync(CancellationToken.None);

            using var recoverScope = _services.CreateScope();
            var scanner = recoverScope.ServiceProvider.GetRequiredService<IInterruptedRecoveryScanner>();
            var requeued = await scanner.ScanAndRequeueAsync(CancellationToken.None);
            Assert.Equal(2, requeued); // Before #8489: 1, the drain-cancelled instance was not requeued.

            await WaitUntilAsync(async () =>
            {
                var current = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = recoverableClient.WorkflowInstanceId });
                return current is { Status: WorkflowStatus.Finished, SubStatus: WorkflowSubStatus.Finished };
            }, TimeSpan.FromSeconds(10));

            var recovered = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = recoverableClient.WorkflowInstanceId });
            Assert.NotNull(recovered);
            Assert.Equal(WorkflowStatus.Finished, recovered.Status);
            Assert.Equal(WorkflowSubStatus.Finished, recovered.SubStatus);
            Assert.Equal(1, _capturingTextWriter.Lines.Count(line => line == "first"));
            Assert.Equal(1, _capturingTextWriter.Lines.Count(line => line == "second"));

            await WaitUntilAsync(async () =>
            {
                var current = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = cooperativeClient.WorkflowInstanceId });
                return current is { Status: WorkflowStatus.Finished, SubStatus: WorkflowSubStatus.Finished };
            }, TimeSpan.FromSeconds(10));
            Assert.Equal(2, _cooperativeGate.Executions);

            var secondScan = await scanner.ScanAndRequeueAsync(CancellationToken.None);
            Assert.Equal(0, secondScan);
        }
        finally
        {
            if (cooperativeRun is not null)
            {
                try { await cooperativeRun.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (Exception ex) when (!ex.IsFatal()) { /* the cut-off runner surfaces OCE */ }
            }

            ResumeGate.Current.Continue.TrySetResult();
            try { await recoverableRun.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception ex) when (!ex.IsFatal()) { /* original cycle may surface OCE after drain */ }
            if (commandProcessor is not null)
                await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(20);
        }

        throw new TimeoutException($"Condition was not met within {timeout}.");
    }
}

/// <summary>Gate for <see cref="DrainLiveActivity"/> so the drain-cancelled workflow stays live until after force-cancel.</summary>
public sealed class DrainLiveGate
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Stays executing until the test releases <see cref="DrainLiveGate.Continue"/>. It ignores its cancellation token, so a
/// drain force-cancel cannot stop it and the cycle stays live.
/// </summary>
public class DrainLiveActivity : CodeActivity
{
    public DrainLiveGate? Gate { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var gate = Gate ?? throw new InvalidOperationException("DrainLiveActivity.Gate must be set.");
        gate.Started.TrySetResult();
        await gate.Continue.Task;
    }
}

/// <summary>Gate shared by <see cref="GatedWaitActivity"/> so the test can persist mid-execution and then release remaining work.</summary>
public sealed class ResumeGate
{
    public static ResumeGate Current { get; set; } = new();

    public int ExecutionCount;
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Blocks without observing cancellation so drain can mark the still-Running row Interrupted.
/// The first execution persists through <see cref="IWorkflowInstanceManager"/>
/// and then stays blocked until the test releases <see cref="ResumeGate.Continue"/>, so it cannot
/// overwrite the recovered Finished row. The restarter's re-entry completes immediately and runs
/// the remaining work.
/// </summary>
public class GatedWaitActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var execution = Interlocked.Increment(ref ResumeGate.Current.ExecutionCount);
        if (execution == 1)
        {
            var manager = context.GetRequiredService<IWorkflowInstanceManager>();
            await manager.SaveAsync(context.WorkflowExecutionContext);
            ResumeGate.Current.Started.TrySetResult();
            await ResumeGate.Current.Continue.Task;
            return;
        }

        ResumeGate.Current.Started.TrySetResult();
    }
}

/// <summary>First step records progress, then a gated wait, then remaining work.</summary>
public class RecoverableResumeWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("first"),
                new GatedWaitActivity(),
                new WriteLine("second")
            }
        };
    }
}
