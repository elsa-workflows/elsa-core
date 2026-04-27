using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Force;

/// <summary>
/// <c>POST /admin/workflow-runtime/force</c> — operator-escalation drain with zero deadline. Cancels every active
/// burst, persists their instances as <see cref="WorkflowSubStatus.Interrupted"/>, and writes a <c>WorkflowInterrupted</c>
/// log entry per affected instance. The host process is NOT exited; the runtime is left in
/// <see cref="QuiescenceReason.Drain"/> until the next runtime generation.
/// </summary>
[PublicAPI]
internal sealed class ForceEndpoint(IWorkflowRuntimeAdminService admin) : ElsaEndpoint<ForceRequest, ForceResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/force");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(ForceRequest req, CancellationToken ct)
    {
        DrainOutcome outcome;
        try
        {
            outcome = await admin.ForceDrainAsync(req.Reason, HttpContext.User.Identity?.Name, ct);
        }
        catch (InvalidOperationException)
        {
            // Non-force drain already in progress — orchestrator rejects parallel runs.
            await Send.ResponseAsync(new ForceResponse(), 409, ct);
            return;
        }

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
