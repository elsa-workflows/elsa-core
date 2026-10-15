using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IWorkflowRuntimeAdminService"/>. Encapsulates the pause/resume/force-drain
/// state-machine semantics and the audit-on-effective-transition rule (SC-007) in one place so transport-specific
/// adapters (HTTP, MCP, CLI) stay thin.
/// </summary>
public sealed class WorkflowRuntimeAdminService(
    IQuiescenceSignal signal,
    IIngressSourceRegistry registry,
    IDrainOrchestrator orchestrator,
    INotificationSender mediator,
    ISystemClock clock) : IWorkflowRuntimeAdminService
{
    /// <inheritdoc />
    public async ValueTask<QuiescenceState> PauseAsync(string? reason, string? requestedBy, CancellationToken cancellationToken)
    {
        var before = signal.CurrentState;
        var after = await signal.PauseAsync(reason, requestedBy, cancellationToken);

        // Audit only effective transitions (SC-007).
        var wasPaused = (before.Reason & QuiescenceReason.AdministrativePause) != 0;
        var isPaused = (after.Reason & QuiescenceReason.AdministrativePause) != 0;
        if (!wasPaused && isPaused)
            await mediator.SendAsync(new RuntimePauseRequested(requestedBy, reason, after.PausedAt ?? clock.UtcNow), cancellationToken);

        return after;
    }

    /// <inheritdoc />
    public async ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken cancellationToken)
    {
        var before = signal.CurrentState;
        var after = await signal.ResumeAsync(requestedBy, cancellationToken);

        var wasPaused = (before.Reason & QuiescenceReason.AdministrativePause) != 0;
        var isPaused = (after.Reason & QuiescenceReason.AdministrativePause) != 0;
        if (wasPaused && !isPaused)
            await mediator.SendAsync(new RuntimeResumeRequested(requestedBy, clock.UtcNow), cancellationToken);

        return after;
    }

    /// <inheritdoc />
    public async ValueTask<DrainOutcome> ForceDrainAsync(string? reason, string? requestedBy, CancellationToken cancellationToken)
    {
        // Caller catches InvalidOperationException to translate to 409 etc.
        var outcome = await orchestrator.DrainAsync(DrainTrigger.OperatorForce, cancellationToken);

        // Audit. Skip when the call returned a cached outcome — repeated invocations in the same generation
        // would otherwise emit spurious audit events and break SC-007 idempotency.
        if (!outcome.WasCached)
            await mediator.SendAsync(new RuntimeForceDrainRequested(requestedBy, reason, clock.UtcNow, outcome), cancellationToken);

        return outcome;
    }

    /// <inheritdoc />
    public RuntimeAdminStatus GetStatus() => new(signal.CurrentState, registry.Snapshot(), signal.ActiveExecutionCycleCount);
}
