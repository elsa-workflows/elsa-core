using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.ForceDrain;

/// <summary>
/// <c>POST /admin/workflow-runtime/force-drain</c> — operator-escalation drain with zero deadline. Cancels every
/// active execution cycle, persists their instances as <see cref="WorkflowSubStatus.Interrupted"/>, and writes a
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
            // Non-force drain already in progress — orchestrator rejects parallel runs. Write the discriminated
            // ConflictResponse directly: Send.ResponseAsync is constrained to the endpoint's TResponse and would
            // force a null-Outcome ForceDrainResponse otherwise.
            HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await HttpContext.Response.WriteAsJsonAsync(
                new ConflictResponse { Code = "drain-in-progress", State = StatusResponseFactory.Build(admin.GetStatus()) },
                ct);
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
        ExecutionCyclesForceCancelledCount = o.ExecutionCyclesForceCancelledCount,
        ForceCancelledInstanceIds = o.ForceCancelledInstanceIds.ToList(),
    };
}
