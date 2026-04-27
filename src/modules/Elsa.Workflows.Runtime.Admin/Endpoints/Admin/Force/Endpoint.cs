using Elsa.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Admin.Endpoints.Admin.Force;

/// <summary>
/// <c>POST /admin/workflow-runtime/force</c> — operator-escalation drain with zero deadline. Cancels every active
/// burst, persists their instances as <see cref="WorkflowSubStatus.Interrupted"/>, and writes a <c>WorkflowInterrupted</c>
/// log entry per affected instance. The host process is NOT exited; the runtime is left in <see cref="QuiescenceReason.Drain"/>
/// until the next runtime generation.
/// </summary>
[PublicAPI]
internal sealed class ForceEndpoint(
    IDrainOrchestrator orchestrator,
    INotificationSender mediator)
    : ElsaEndpoint<ForceRequest, ForceResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/force");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(ForceRequest req, CancellationToken ct)
    {
        var requestedBy = HttpContext.User.Identity?.Name;
        DrainOutcome outcome;

        try
        {
            outcome = await orchestrator.DrainAsync(DrainTrigger.OperatorForce, ct);
        }
        catch (InvalidOperationException)
        {
            // A non-force drain is already in progress — drain orchestrator rejects parallel runs.
            await Send.ResponseAsync(new ForceResponse(), 409, ct);
            return;
        }

        // Audit. Skip when the call returned a cached outcome (i.e., a previous force already ran in this generation),
        // otherwise repeated POST /force calls would emit spurious audit events and break SC-007 idempotency.
        if (!outcome.WasCached)
            await mediator.SendAsync(new RuntimeForceRequested(requestedBy, req.Reason, DateTimeOffset.UtcNow, outcome), ct);

        await Send.OkAsync(new ForceResponse { Outcome = MapOutcome(outcome) }, ct);
    }

    private static DrainOutcomeDto MapOutcome(DrainOutcome o) => new()
    {
        OverallResult = o.OverallResult.ToString(),
        StartedAt = o.StartedAt,
        CompletedAt = o.CompletedAt,
        PausePhaseDuration = o.PausePhaseDuration,
        WaitPhaseDuration = o.WaitPhaseDuration,
        Sources = o.Sources.Select(s => new IngressSourceStateDto
        {
            Name = s.Name,
            State = s.State.ToString(),
            LastError = s.LastError?.Message,
        }).ToList(),
        BurstsForceCancelledCount = o.BurstsForceCancelledCount,
        ForceCancelledInstanceIds = o.ForceCancelledInstanceIds.ToList(),
    };
}
