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
/// 3. Wait for <see cref="IExecutionCycleRegistry.ActiveCount"/> to reach zero, polling on a short interval.
/// 4. On deadline breach (or on operator force, where deadline is zero), iterate live cycles, cancel each,
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
    private readonly IExecutionCycleRegistry _cycles;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<GracefulShutdownOptions> _options;
    private readonly IOptions<HostOptions> _hostOptions;
    private readonly ISystemClock _clock;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ILogger<DrainOrchestrator> _logger;

    private readonly object _sync = new();
    private DrainOutcome? _previousOutcome;
    private bool _drainInProgress;

    public DrainOrchestrator(
        IQuiescenceSignal signal,
        IIngressSourceRegistry registry,
        IExecutionCycleRegistry cycles,
        IServiceScopeFactory scopeFactory,
        IOptions<GracefulShutdownOptions> options,
        IOptions<HostOptions> hostOptions,
        ISystemClock clock,
        IIdentityGenerator identityGenerator,
        ILogger<DrainOrchestrator> logger)
    {
        _signal = signal;
        _registry = registry;
        _cycles = cycles;
        _scopeFactory = scopeFactory;
        _options = options;
        _hostOptions = hostOptions;
        _clock = clock;
        _identityGenerator = identityGenerator;
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
                    return _previousOutcome with { WasCached = true };
                throw new InvalidOperationException($"Drain already in progress; second invocation rejected (trigger={trigger}).");
            }
            if (_previousOutcome is not null)
            {
                if (trigger == DrainTrigger.OperatorForce) return _previousOutcome with { WasCached = true };
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

            var deadlineAt = startedAt + deadline;

            // Phase 1: parallel pause. Each source has its own timeout independent of the overall deadline,
            // but force-stop fallbacks are clamped to the *remaining* overall budget (deadlineAt - now), not
            // the full TimeSpan, so a slow per-source pause can't extend the total shutdown window.
            await PauseAllSourcesAsync(deadlineAt, cancellationToken);
            pausePhase = sw.Elapsed;
            _logger.LogInformation("Ingress pause phase complete in {Elapsed}.", pausePhase);

            // Phase 2: wait for active execution cycles to drain.
            var waitStart = sw.Elapsed;
            var deadlineBreach = !await WaitForCyclesAsync(deadlineAt, cancellationToken);
            waitPhase = sw.Elapsed - waitStart;

            // Phase 3: outcome assembly.
            int forceCancelled = 0;
            IReadOnlyList<string> forceCancelledIds = Array.Empty<string>();

            if (deadlineBreach || trigger == DrainTrigger.OperatorForce)
            {
                _logger.LogWarning("Drain {What}; force-cancelling {Count} active execution cycle(s).",
                    deadlineBreach ? "deadline exceeded" : "operator-forced", _cycles.ActiveCount);
                (forceCancelled, forceCancelledIds) = await ForceCancelActiveCyclesAsync(trigger, _signal.CurrentState.GenerationId, cancellationToken);
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
                ExecutionCyclesForceCancelledCount: forceCancelled,
                ForceCancelledInstanceIds: forceCancelledIds);

            lock (_sync) _previousOutcome = outcome;
            _logger.LogInformation("Drain completed: {Result} (paused={Paused}, waited={Waited}, forceCancelled={ForceCancelled}).",
                outcome.OverallResult, outcome.PausePhaseDuration, outcome.WaitPhaseDuration, outcome.ExecutionCyclesForceCancelledCount);
            return outcome;
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            // The "drain already in progress / completed" InvalidOperationExceptions are thrown OUTSIDE this
            // try block (during the lock-protected setup), so they bubble out without triggering this handler.
            // Any IOE that lands here is incidental — from a store/dispatcher inside the protocol — and should
            // be captured into the outcome rather than escaping the whole drain.
            _logger.LogError(ex, "Drain aborted by unhandled exception.");
            var outcome = new DrainOutcome(
                OverallResult: DrainResult.AbortedByUnhandledException,
                StartedAt: startedAt,
                CompletedAt: _clock.UtcNow,
                PausePhaseDuration: pausePhase,
                WaitPhaseDuration: waitPhase,
                Sources: BuildSourceFinalStates(),
                ExecutionCyclesForceCancelledCount: 0,
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

    private async Task PauseAllSourcesAsync(DateTimeOffset deadlineAt, CancellationToken cancellationToken)
    {
        var sources = _registry.Sources;
        if (sources.Count == 0) return;

        var pauseTasks = sources.Select(source => PauseOneSourceAsync(source, deadlineAt, cancellationToken)).ToArray();
        await Task.WhenAll(pauseTasks);
    }

    private async Task PauseOneSourceAsync(IIngressSource source, DateTimeOffset deadlineAt, CancellationToken cancellationToken)
    {
        // Precedence: a positive per-source PauseTimeout wins; Zero (or negative) defers to the
        // configured GracefulShutdownOptions.IngressPauseTimeout. The resolved value is then
        // clamped to the overall remaining budget, with a 1 ms floor so a misconfigured zero
        // configured-default still produces a non-zero CancelAfter.
        var configured = _options.Value.IngressPauseTimeout;
        var sourceDeadline = source.PauseTimeout > TimeSpan.Zero ? source.PauseTimeout : configured;
        var overallRemaining = deadlineAt - _clock.UtcNow;
        if (sourceDeadline > overallRemaining) sourceDeadline = overallRemaining;
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
            await TryForceStopAsync(source, deadlineAt, cancellationToken);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            await _registry.MarkPauseFailedAsync(source.Name, "exception", ex);
            await TryForceStopAsync(source, deadlineAt, cancellationToken);
        }
    }

    private async Task TryForceStopAsync(IIngressSource source, DateTimeOffset deadlineAt, CancellationToken cancellationToken)
    {
        if (source is not IForceStoppable forceStoppable) return;

        // Use the *remaining* budget — not the full overall deadline — so a per-source pause that already
        // burned most of the window can't get another full deadline's worth of force-stop runway.
        var remaining = deadlineAt - _clock.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            _logger.LogWarning("Skipping force-stop of ingress source '{Name}': overall drain deadline already exceeded.", source.Name);
            return;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(remaining);
            await forceStoppable.ForceStopAsync(cts.Token);
            _logger.LogInformation("Force-stopped ingress source '{Name}'.", source.Name);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            _logger.LogWarning(ex, "Force-stop of ingress source '{Name}' failed; drain continues.", source.Name);
        }
    }

    private async Task<bool> WaitForCyclesAsync(DateTimeOffset deadlineAt, CancellationToken cancellationToken)
    {
        while (_cycles.ActiveCount > 0)
        {
            if (_clock.UtcNow >= deadlineAt) return false;
            try { await Task.Delay(PollInterval, cancellationToken); }
            catch (OperationCanceledException) { return _cycles.ActiveCount == 0; }
        }
        return true;
    }

    /// <summary>Bound on how long the orchestrator waits for a runner to settle after invoking execution cycle cancellation.</summary>
    private static readonly TimeSpan ForceCancelSettleTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Per-handle bound on the forensic Interrupted persist write in Phase C. Decoupled from the drain
    /// cancellation token: when the host's drain CT has already fired, the persist still has up to this long
    /// to land the row + log entry. Keeps "proceed to persist anyway" honest while preventing a stuck DB
    /// from hanging shutdown indefinitely.
    /// </summary>
    private static readonly TimeSpan PersistInterruptedTimeout = TimeSpan.FromSeconds(5);

    private async Task<(int Count, IReadOnlyList<string> Ids)> ForceCancelActiveCyclesAsync(DrainTrigger trigger, string generationId, CancellationToken cancellationToken)
    {
        var cap = _options.Value.MaxForceCancelledInstanceIdsReported;
        var reportedIds = new List<string>(capacity: Math.Min(cap, 16));
        var totalCancelled = 0;

        var live = _cycles.ListActiveCycles();
        if (live.Count == 0) return (0, Array.Empty<string>());

        using var scope = _scopeFactory.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var logStore = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionLogStore>();

        var reason = trigger == DrainTrigger.OperatorForce
            ? WorkflowInterruptedPayload.ReasonOperatorForce
            : WorkflowInterruptedPayload.ReasonDeadlineBreach;

        // Force-cancel proceeds in three phases. The split exists because cancelling and
        // awaiting in the same loop made every execution cycle after the first run at full speed
        // through the prior execution cycle's settle window — total wall time was O(N × settle
        // timeout), defeating the intent of force-cancel under concurrency.
        //
        // Phase A — cancel every handle synchronously. CancellationTokenSource.Cancel is
        // cheap and we want every runner to observe cancellation simultaneously rather
        // than serialized behind preceding settle waits.
        foreach (var handle in live)
        {
            try
            {
                handle.Cancel();
                totalCancelled++;
                if (reportedIds.Count < cap) reportedIds.Add(handle.WorkflowInstanceId);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.LogError(ex, "Failed to cancel execution cycle {ExecutionCycleId} (instance={InstanceId}).", handle.Id, handle.WorkflowInstanceId);
            }
        }

        // Phase B — wait for every runner to settle in parallel under a shared deadline.
        // The handle disposes when ExecutionCycleTrackingMiddleware exits its `using` block, which
        // is after the workflow runner has finished its commit. We want runners' terminal
        // commits to land before we overwrite the sub-status with Interrupted — but we
        // bound the wait so a non-cancellable activity cannot block drain, accepting the
        // runner-clobber race for that one instance (the recovery scan picks it up).
        // Total wall time for this phase is at most ForceCancelSettleTimeout regardless
        // of N.
        var settleTasks = live.Select(async handle =>
        {
            try
            {
                await handle.Disposed.WaitAsync(ForceCancelSettleTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Execution cycle {ExecutionCycleId} (instance={InstanceId}) did not settle within {Timeout}; persisting Interrupted now (the runner may overwrite — recovery scan picks it up).", handle.Id, handle.WorkflowInstanceId, ForceCancelSettleTimeout);
            }
            catch (OperationCanceledException) { /* drain CT fired — proceed to persist anyway */ }
        });
        await Task.WhenAll(settleTasks).ConfigureAwait(false);

        // Phase C — persist Interrupted for every handle. Sequential to keep DbContext
        // usage single-threaded; per-handle persistence is small.
        //
        // Each persist runs under its own bounded token that is NOT linked to the drain CT.
        // Phase B's catch on OperationCanceledException explicitly comments "drain CT fired —
        // proceed to persist anyway"; reusing the cancelled token here would have made that
        // a lie — the very first await inside PersistInterruptedAsync would observe the
        // cancelled token and throw. Result: every execution cycle would be left in an unrecovered
        // executing state on host shutdown. The bounded non-drain token preserves the
        // forensic write while preventing a stuck DB from hanging shutdown indefinitely.
        foreach (var handle in live)
        {
            try
            {
                using var persistCts = new CancellationTokenSource(PersistInterruptedTimeout);
                await PersistInterruptedAsync(instanceStore, logStore, handle, generationId, reason, persistCts.Token);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.LogError(ex, "Failed to persist Interrupted for execution cycle {ExecutionCycleId} (instance={InstanceId}).", handle.Id, handle.WorkflowInstanceId);
            }
        }

        return (totalCancelled, reportedIds);
    }

    private async Task PersistInterruptedAsync(
        IWorkflowInstanceStore instanceStore,
        IWorkflowExecutionLogStore logStore,
        ExecutionCycleHandle handle,
        string generationId,
        string reason,
        CancellationToken cancellationToken)
    {
        var instance = await instanceStore.FindAsync(new WorkflowInstanceFilter { Id = handle.WorkflowInstanceId }, cancellationToken);

        if (instance is null)
        {
            // The instance row was never persisted (e.g., a execution cycle whose runner never reached commitStateHandler).
            // We cannot update a row that doesn't exist, but we MUST still write the forensic trail so postmortem
            // recovery can audit what happened. Write a synthetic WorkflowInterrupted log entry with the
            // PersistenceFailure discriminator — the workflow-definition fields are unknown so they remain empty.
            var orphanPayload = new WorkflowInterruptedPayload(
                InterruptedAt: _clock.UtcNow,
                Reason: WorkflowInterruptedPayload.ReasonPersistenceFailure,
                GenerationId: generationId,
                LastActivityId: null,
                LastActivityNodeId: null,
                IngressSourceName: handle.IngressSourceName,
                ExecutionCycleDuration: _clock.UtcNow - handle.StartedAt);
            try
            {
                await logStore.AddAsync(new Entities.WorkflowExecutionLogRecord
                {
                    Id = _identityGenerator.GenerateId(),
                    WorkflowInstanceId = handle.WorkflowInstanceId,
                    WorkflowDefinitionId = string.Empty,
                    WorkflowDefinitionVersionId = string.Empty,
                    WorkflowVersion = 0,
                    ActivityInstanceId = string.Empty,
                    ActivityId = string.Empty,
                    ActivityType = string.Empty,
                    ActivityNodeId = string.Empty,
                    Timestamp = orphanPayload.InterruptedAt,
                    EventName = WorkflowInterruptedPayload.WorkflowInterruptedEventName,
                    Source = "Elsa.Workflows.Runtime.GracefulShutdown",
                    Message = $"Workflow execution cycle force-cancelled by the runtime ({orphanPayload.Reason}); no instance row was persisted at the time of cancellation.",
                    Payload = orphanPayload,
                }, cancellationToken);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.LogWarning(ex, "Failed to write WorkflowInterrupted log entry for orphan execution cycle {ExecutionCycleId} (instance={InstanceId}).", handle.Id, handle.WorkflowInstanceId);
            }
            return;
        }

        var actualReason = reason;

        try
        {
            instance.SubStatus = WorkflowSubStatus.Interrupted;
            instance.IsExecuting = false;
            await instanceStore.SaveAsync(instance, cancellationToken);
        }
        catch (Exception ex) when (!ex.IsFatal())
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
            ExecutionCycleDuration: _clock.UtcNow - handle.StartedAt);

        try
        {
            await logStore.LogWorkflowInterruptedAsync(_identityGenerator, instance, payload, cancellationToken);
        }
        catch (Exception ex) when (!ex.IsFatal())
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
