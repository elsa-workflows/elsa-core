using Elsa.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Pause;

/// <summary>
/// <c>POST /admin/workflow-runtime/pause</c> — places the runtime into <see cref="QuiescenceReason.AdministrativePause"/>.
/// Idempotent: subsequent calls return the current state without producing additional audit events (SC-007).
/// </summary>
[PublicAPI]
internal sealed class PauseEndpoint(
    IQuiescenceSignal signal,
    IIngressSourceRegistry registry,
    INotificationSender mediator)
    : ElsaEndpoint<PauseRequest, StatusResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/pause");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(PauseRequest req, CancellationToken ct)
    {
        var requestedBy = HttpContext.User.Identity?.Name;
        var before = signal.CurrentState;
        var after = await signal.PauseAsync(req.Reason, requestedBy, ct);

        // Audit only effective transitions (SC-007).
        var wasPaused = (before.Reason & QuiescenceReason.AdministrativePause) != 0;
        var isPaused = (after.Reason & QuiescenceReason.AdministrativePause) != 0;
        if (!wasPaused && isPaused)
            await mediator.SendAsync(new RuntimePauseRequested(requestedBy, req.Reason, after.PausedAt ?? DateTimeOffset.UtcNow), ct);

        await Send.OkAsync(StatusResponseFactory.Build(signal, registry), ct);
    }
}
