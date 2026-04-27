using System.Diagnostics;
using Elsa.Common;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IDrainOrchestrator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Protocol per <c>contracts/drain-orchestrator.md</c>:
/// 1. Enter drain via <see cref="IQuiescenceSignal.BeginDrainAsync"/>.
/// 2. Pause every registered ingress source in parallel, each bounded by its own per-source timeout. Sources that
///    fail to pause are recorded as <see cref="IngressSourceState.PauseFailed"/> and, if they implement
///    <see cref="IForceStoppable"/>, are escalated.
/// 3. Wait for <see cref="IBurstRegistry.ActiveCount"/> to reach zero, polling on a short interval.
/// 4. On deadline breach (or on operator force, where deadline is zero), iterate live bursts, cancel each,
///    persist the corresponding instance in <see cref="WorkflowSubStatus.Interrupted"/>, and write a
///    <c>WorkflowInterrupted</c> entry in the per-instance execution log.
/// 5. Return a <see cref="DrainOutcome"/>.
/// </para>
/// <para>
/// All exceptions inside the protocol are captured into the outcome — only an <see cref="InvalidOperationException"/>
/// thrown by a second non-force invocation propagates.
/// </para>
/// </remarks>
public sealed class DrainOrchestrator : IDrainOrchestrator
{
    private static readonly TimeSpan SafetyEpsilon = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    private readonly IQuiescenceSignal _signal;
    private readonly IIngressSourceRegistry _registry;
    private readonly IBurstRegistry _bursts;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<GracefulShutdownOptions> _options;
    private readonly IOptions<HostOptions> _hostOptions;
    private readonly ISystemClock _clock;
    private readonly ILogger<DrainOrchestrator> _logger;

    private readonly object _sync = new();
    private DrainOutcome? _previousOutcome;
    private bool _drainInProgress;

    public DrainOrchestrator(
        IQuiescenceSignal signal,
        IIngressSourceRegistry registry,
        IBurstRegistry bursts,
        IServiceScopeFactory scopeFactory,
        IOptions<GracefulShutdownOptions> options,
        IOptions<HostOptions> hostOptions,
        ISystemClock clock,
        ILogger<DrainOrchestrator> logger)
    {
        _signal = signal;
        _registry = registry;
        _bursts = bursts;
        _scopeFactory = scopeFactory;
        _options = options;
        _hostOptions = hostOptions;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<DrainOutcome> DrainAsync(DrainTrigger trigger, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_drainInProgress)
            {
                if (trigger == DrainTrigger.OperatorForce && _previousOutcome is not null)
                    return _previousOutcome;
                throw new InvalidOperationException($"Drain already in progress; second invocation rejected (trigger={trigger}).");
            }
            if (_previousOutcome is not null)
            {
                if (trigger == DrainTrigger.OperatorForce) return _previousOutcome;
                throw new InvalidOperationException("Drain already completed in this generation; subsequent non-force invocations are rejected.");
            }
            _drainInProgress = true;
        }

        var startedAt = _clock.UtcNow;
        var deadline = ComputeEffectiveDeadline(trigger);
        var sw = Stopwatch.StartNew();
        TimeSpan pausePhase = TimeSpan.Zero;
        TimeSpan waitPhase = TimeSpan.Zero;

        try
        {
            await _signal.BeginDrainAsync(cancellationToken);
            _logger.LogInformation("Drain initiated (trigger={Trigger}, deadline={Deadline}).", trigger, deadline);

            // Phase 1: parallel pause. Each source has its own timeout independent of the overall deadline.
            await PauseAllSourcesAsync(deadline, cancellationToken);
            pausePhase = sw.Elapsed;
            _logger.LogInformation("Ingress pause phase complete in {Elapsed}.", pausePhase);

            // Phase 2: wait for active bursts to drain.
            var waitStart = sw.Elapsed;
            var deadlineAt = startedAt + deadline;
            var deadlineBreach = !await WaitForBurstsAsync(deadlineAt, cancellationToken);
            waitPhase = sw.Elapsed - waitStart;

            // Phase 3: outcome assembly.
            int forceCancelled = 0;
            IReadOnlyList<string> forceCancelledIds = Array.Empty<string>();

            if (deadlineBreach || trigger == DrainTrigger.OperatorForce)
            {
                _logger.LogWarning("Drain {What}; force-cancelling {Count} active burst(s).",
                    deadlineBreach ? "deadline exceeded" : "operator-forced", _bursts.ActiveCount);
                (forceCancelled, forceCancelledIds) = await ForceCancelActiveBurstsAsync(trigger, _signal.CurrentState.GenerationId, cancellationToken);
            }

            // OperatorForce always reports Forced regardless of zero-deadline breach mechanics.
            var result = trigger switch
            {
                DrainTrigger.OperatorForce => DrainResult.Forced,
                _ when deadlineBreach => DrainResult.DeadlineExceeded,
                _ => DrainResult.CompletedWithinDeadline,
            };

            var outcome = new DrainOutcome(
                OverallResult: result,
                StartedAt: startedAt,
                CompletedAt: _clock.UtcNow,
                PausePhaseDuration: pausePhase,
                WaitPhaseDuration: waitPhase,
                Sources: BuildSourceFinalStates(),
                BurstsForceCancelledCount: forceCancelled,
                ForceCancelledInstanceIds: forceCancelledIds);

            lock (_sync) _previousOutcome = outcome;
            _logger.LogInformation("Drain completed: {Result} (paused={Paused}, waited={Waited}, forceCancelled={ForceCancelled}).",
                outcome.OverallResult, outcome.PausePhaseDuration, outcome.WaitPhaseDuration, outcome.BurstsForceCancelledCount);
            return outcome;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Drain aborted by unhandled exception.");
            var outcome = new DrainOutcome(
                OverallResult: DrainResult.AbortedByUnhandledException,
                StartedAt: startedAt,
                CompletedAt: _clock.UtcNow,
                PausePhaseDuration: pausePhase,
                WaitPhaseDuration: waitPhase,
                Sources: BuildSourceFinalStates(),
                BurstsForceCancelledCount: 0,
                ForceCancelledInstanceIds: Array.Empty<string>());
            lock (_sync) _previousOutcome = outcome;
            return outcome;
        }
        finally
        {
            lock (_sync) _drainInProgress = false;
        }
    }

    private TimeSpan ComputeEffectiveDeadline(DrainTrigger trigger)
    {
        if (trigger == DrainTrigger.OperatorForce) return TimeSpan.Zero;

        var configured = _options.Value.DrainDeadline;
        var hostShutdown = _hostOptions.Value.ShutdownTimeout;

        // Clamp to host's own shutdown budget minus a safety epsilon, so persistence can finish before the process is killed.
        if (hostShutdown > SafetyEpsilon)
        {
            var hostBudget = hostShutdown - SafetyEpsilon;
            if (hostBudget < configured) return hostBudget;
        }

        return configured;
    }

    private async Task PauseAllSourcesAsync(TimeSpan overallDeadline, CancellationToken cancellationToken)
    {
        var sources = _registry.Sources;
        if (sources.Count == 0) return;

        var pauseTasks = sources.Select(source => PauseOneSourceAsync(source, overallDeadline, cancellationToken)).ToArray();
        await Task.WhenAll(pauseTasks);
    }

    private async Task PauseOneSourceAsync(IIngressSource source, TimeSpan overallDeadline, CancellationToken cancellationToken)
    {
        var sourceDeadline = source.PauseTimeout > overallDeadline ? overallDeadline : source.PauseTimeout;
        if (sourceDeadline <= TimeSpan.Zero) sourceDeadline = TimeSpan.FromMilliseconds(1);

        _registry.RecordTransition(source.Name, IngressSourceState.Pausing);
        using var perSourceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        perSourceCts.CancelAfter(sourceDeadline);

        try
        {
            await source.PauseAsync(perSourceCts.Token).AsTask().WaitAsync(perSourceCts.Token);
            _registry.RecordTransition(source.Name, IngressSourceState.Paused);
        }
        catch (OperationCanceledException) when (perSourceCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            await _registry.MarkPauseFailedAsync(source.Name, "timeout", new TimeoutException($"Source '{source.Name}' did not pause within {sourceDeadline}."));
            await TryForceStopAsync(source, overallDeadline, cancellationToken);
        }
        catch (Exception ex)
        {
            await _registry.MarkPauseFailedAsync(source.Name, "exception", ex);
            await TryForceStopAsync(source, overallDeadline, cancellationToken);
        }
    }

    private async Task TryForceStopAsync(IIngressSource source, TimeSpan deadline, CancellationToken cancellationToken)
    {
        if (source is not IForceStoppable forceStoppable) return;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(deadline);
            await forceStoppable.ForceStopAsync(cts.Token);
            _logger.LogInformation("Force-stopped ingress source '{Name}'.", source.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Force-stop of ingress source '{Name}' failed; drain continues.", source.Name);
        }
    }

    private async Task<bool> WaitForBurstsAsync(DateTimeOffset deadlineAt, CancellationToken cancellationToken)
    {
        while (_bursts.ActiveCount > 0)
        {
            if (_clock.UtcNow >= deadlineAt) return false;
            try { await Task.Delay(PollInterval, cancellationToken); }
            catch (OperationCanceledException) { return _bursts.ActiveCount == 0; }
        }
        return true;
    }

    /// <summary>Bound on how long the orchestrator waits for a runner to settle after invoking burst cancellation.</summary>
    private static readonly TimeSpan ForceCancelSettleTimeout = TimeSpan.FromSeconds(2);

    private async Task<(int Count, IReadOnlyList<string> Ids)> ForceCancelActiveBurstsAsync(DrainTrigger trigger, string generationId, CancellationToken cancellationToken)
    {
        var cap = _options.Value.MaxForceCancelledInstanceIdsReported;
        var reportedIds = new List<string>(capacity: Math.Min(cap, 16));
        var totalCancelled = 0;

        var live = _bursts.EnumerateActive();
        if (live.Count == 0) return (0, Array.Empty<string>());

        using var scope = _scopeFactory.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var logStore = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionLogStore>();

        foreach (var handle in live)
        {
            try
            {
                handle.Cancel();
                totalCancelled++;
                if (reportedIds.Count < cap) reportedIds.Add(handle.WorkflowInstanceId);

                // Wait for the runner to settle so its terminal commit happens before we overwrite the sub-status with
                // Interrupted. The handle disposes when BurstTrackingMiddleware exits its `using` block, which is after
                // the workflow runner has finished its commit. The wait is bounded so a non-cancellable activity does
                // not block drain — on timeout we proceed and accept the runner-clobber race for that one instance.
                try
                {
                    await handle.Disposed.WaitAsync(ForceCancelSettleTimeout, cancellationToken);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Burst {BurstId} (instance={InstanceId}) did not settle within {Timeout}; persisting Interrupted now (the runner may overwrite — recovery scan picks it up).", handle.Id, handle.WorkflowInstanceId, ForceCancelSettleTimeout);
                }
                catch (OperationCanceledException) { /* drain CT fired — proceed to persist anyway */ }

                var reason = trigger == DrainTrigger.OperatorForce
                    ? WorkflowInterruptedPayload.ReasonOperatorForce
                    : WorkflowInterruptedPayload.ReasonDeadlineBreach;

                await PersistInterruptedAsync(instanceStore, logStore, handle, generationId, reason, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to force-cancel burst {BurstId} (instance={InstanceId}).", handle.Id, handle.WorkflowInstanceId);
            }
        }

        return (totalCancelled, reportedIds);
    }

    private async Task PersistInterruptedAsync(
        IWorkflowInstanceStore instanceStore,
        IWorkflowExecutionLogStore logStore,
        BurstHandle handle,
        string generationId,
        string reason,
        CancellationToken cancellationToken)
    {
        var instance = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = handle.WorkflowInstanceId }, cancellationToken);

        if (instance is null)
        {
            // No instance row to update and no instance metadata to populate the WorkflowInterrupted log entry's
            // definition fields with. The burst handle's metadata (BurstId, WorkflowInstanceId, IngressSourceName,
            // BurstDuration) is still in the orchestrator's logged drain outcome — that's the forensic trail for
            // this case. The next runtime generation's recovery scan will see no row matching the instance ID and
            // skip; if the row appears later (eventual-consistency on a sibling node), the existing timeout-based
            // RestartInterruptedWorkflowsTask handles it.
            _logger.LogWarning("Burst {BurstId} for instance {InstanceId} cancelled but no instance row found; forensic detail captured in the drain outcome only.", handle.Id, handle.WorkflowInstanceId);
            return;
        }

        var actualReason = reason;

        try
        {
            instance.SubStatus = WorkflowSubStatus.Interrupted;
            instance.IsExecuting = false;
            await instanceStore.SaveAsync(instance, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist Interrupted sub-status for instance {InstanceId}; falling back to PersistenceFailure reason.", instance.Id);
            actualReason = WorkflowInterruptedPayload.ReasonPersistenceFailure;
        }

        var payload = new WorkflowInterruptedPayload(
            InterruptedAt: _clock.UtcNow,
            Reason: actualReason,
            GenerationId: generationId,
            LastActivityId: null,
            LastActivityNodeId: null,
            IngressSourceName: handle.IngressSourceName,
            BurstDuration: _clock.UtcNow - handle.StartedAt);

        try
        {
            await logStore.LogWorkflowInterruptedAsync(instance, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write WorkflowInterrupted log entry for instance {InstanceId}.", instance.Id);
        }
    }

    private IReadOnlyList<IngressSourceFinalState> BuildSourceFinalStates()
    {
        return _registry.Snapshot()
            .Select(s => new IngressSourceFinalState(s.Name, s.State, s.LastError, WasForceStopped: s.State == IngressSourceState.PauseFailed && s.LastError is not null))
            .ToArray();
    }
}
