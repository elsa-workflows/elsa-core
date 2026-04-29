using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.ForceDrain;

/// <summary>
/// <c>POST /admin/workflow-runtime/force-drain</c> — operator-escalation drain with zero deadline. Cancels every
/// active burst, persists their instances as <see cref="WorkflowSubStatus.Interrupted"/>, and writes a
/// <c>WorkflowInterrupted</c> log entry per affected instance. The host process is NOT exited; the runtime is left
/// in <see cref="QuiescenceReason.Drain"/> until the next runtime generation.
/// </summary>
[PublicAPI]
internal sealed class ForceDrainEndpoint(IWorkflowRuntimeAdminService admin) : ElsaEndpoint<ForceDrainRequest, ForceDrainResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/force-drain");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(ForceDrainRequest req, CancellationToken ct)
    {
        DrainOutcome outcome;
        try
        {
            outcome = await admin.ForceDrainAsync(req.Reason, HttpContext.User.Identity?.Name, ct);
        }
        catch (InvalidOperationException)
        {
            // Non-force drain already in progress — orchestrator rejects parallel runs.
            await Send.ResponseAsync(new ForceDrainResponse(), 409, ct);
            return;
        }

        await Send.OkAsync(new ForceDrainResponse { Outcome = MapOutcome(outcome) }, ct);
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
