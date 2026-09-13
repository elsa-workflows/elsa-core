using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// End-to-end test for the drain orchestrator's deadline-breach path against a real workflow execution. A workflow
/// containing a slow activity is started on a background task; the orchestrator is invoked mid-flight with a
/// sub-activity-duration deadline; the test asserts the workflow lands in <see cref="WorkflowSubStatus.Interrupted"/>
/// and a <c>WorkflowInterrupted</c> forensic log entry is written.
///
/// This is the integration-level companion to the unit tests in <c>DrainOrchestratorWaitTests</c> — it exercises the
/// production DI graph, the real <see cref="Pipelines.WorkflowExecution.IWorkflowExecutionPipeline"/>, and the
/// <c>ExecutionCycleHandle.Disposed</c>-await sequencing that ensures the orchestrator's <c>Interrupted</c> persistence wins
/// the race against the workflow runner's terminal commit.
/// </summary>
public class DeadlineBreachEndToEndTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IDrainOrchestrator _orchestrator;

    public DeadlineBreachEndToEndTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .AddActivitiesFrom<DeadlineBreachEndToEndTests>()
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(o =>
                {
                    // Sub-activity-duration deadline forces the orchestrator down the deadline-breach path.
                    o.DrainDeadline = TimeSpan.FromMilliseconds(50);
                    o.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                })))
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
        _orchestrator = _services.GetRequiredService<IDrainOrchestrator>();
    }

    [Test]
    [DisplayName("Drain deadline breach against a running workflow persists the instance as Interrupted with a WorkflowInterrupted log entry")]
    public async Task DeadlineBreachPersistsInterrupted()
    {
        await _services.PopulateRegistriesAsync();

        var activityState = new ObservableActivityState();
        var workflow = new TestWorkflow(builder => builder.Root = new ObservableActivity
        {
            State = activityState,
            DelayMs = 500
        });

        using var workflowCancellation = new CancellationTokenSource();

        // Start the workflow on a background task. Its activity runs well beyond the configured drain deadline.
        var workflowTask = Task.Run(() => _workflowRunner.RunAsync(workflow, cancellationToken: workflowCancellation.Token));

        try
        {
            // Wait until the activity has actually started executing.
            await activityState.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Trigger drain mid-flight: 50 ms deadline, 500 ms activity guarantees the deadline-breach path.
            var outcome = await _orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

            // Wait for the workflow runner to fully unwind.
            try { await workflowTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) { /* Force-cancellation may surface from the runner. */ }

            // Drain reports a deadline breach with exactly one force-cancelled execution cycle.
            await Assert.That(outcome.OverallResult).IsEqualTo(DrainResult.DeadlineExceeded);
            await Assert.That(outcome.ExecutionCyclesForceCancelledCount).IsEqualTo(1);
            await Assert.That(outcome.ForceCancelledInstanceIds).HasSingleItem();

            // The workflow instance ends up persisted as Interrupted. The ExecutionCycleAwareCommitStateHandler decorator
            // disposes the execution cycle handle AFTER the runner's commit, so the orchestrator's await-disposed sequencing
            // correctly lands the Interrupted write last (no runner-clobber).
            using var scope = _services.CreateScope();
            var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var interruptedInstances = (await instanceStore.FindManyAsync(
                new WorkflowInstanceFilter { WorkflowSubStatus = WorkflowSubStatus.Interrupted },
                CancellationToken.None)).ToList();

            await Assert.That(interruptedInstances).IsNotEmpty();
            await Assert.That(interruptedInstances[0].IsExecuting).IsFalse().Because("An Interrupted instance must have IsExecuting=false so the existing timeout-based crash recovery does not also pick it up.");

            // A WorkflowInterrupted forensic log entry was written for the force-cancelled execution cycle.
            var logStore = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionLogStore>();
            var logRecords = await logStore.FindManyAsync(
                new WorkflowExecutionLogRecordFilter(),
                PageArgs.FromPage(0, 100),
                CancellationToken.None);
            var interruptedLog = logRecords.Items.FirstOrDefault(r => r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName);

            await Assert.That(interruptedLog).IsNotNull();
            await Assert.That(interruptedLog!.Payload).IsOfType(typeof(WorkflowInterruptedPayload));
            var payload = (WorkflowInterruptedPayload)interruptedLog.Payload!;
            await Assert.That(payload.Reason).IsEqualTo(WorkflowInterruptedPayload.ReasonDeadlineBreach);
        }
        finally
        {
            if (!workflowTask.IsCompleted)
            {
                await workflowCancellation.CancelAsync();
                try { await workflowTask.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (Exception exception) when (!exception.IsFatal()) { /* Preserve the original test failure while still making a bounded cleanup attempt. */ }
            }
        }
    }

    [Test]
    [DisplayName("Drain that completes within deadline reports CompletedWithinDeadline (success path baseline)")]
    public async Task SuccessPathBaseline()
    {
        await _services.PopulateRegistriesAsync();

        // No workflow running — drain finds zero active execution cycles and returns immediately.
        var outcome = await _orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

        await Assert.That(outcome.OverallResult).IsEqualTo(DrainResult.CompletedWithinDeadline);
        await Assert.That(outcome.ExecutionCyclesForceCancelledCount).IsEqualTo(0);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

/// <summary>Shared state for <see cref="ObservableActivity"/>: signals when execution started and the natural-completion / cancelled outcome.</summary>
public sealed class ObservableActivityState
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// A test activity that signals entry, then runs long enough for the drain deadline to expire while observing its
/// <see cref="ActivityExecutionContext.CancellationToken"/>.
/// </summary>
public class ObservableActivity : CodeActivity
{
    /// <summary>Delay in milliseconds; an integer keeps the activity serializable.</summary>
    public int DelayMs { get; set; }

    /// <summary>Out-of-band signalling channel for the test (set by the test before scheduling).</summary>
    public ObservableActivityState? State { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        State?.Started.TrySetResult();
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(DelayMs), context.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            State?.Cancelled.TrySetResult();
        }
    }
}
