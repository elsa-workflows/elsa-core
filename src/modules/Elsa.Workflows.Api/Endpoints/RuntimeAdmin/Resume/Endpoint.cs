using Elsa.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Resume;

/// <summary>
/// <c>POST /admin/workflow-runtime/resume</c> — clears <see cref="QuiescenceReason.AdministrativePause"/>.
/// Returns 409 Conflict when a drain is in progress (resume is meaningless mid-drain — see edge case in spec).
/// Idempotent on the success path: a resume on an already-running runtime returns the current state.
/// </summary>
[PublicAPI]
internal sealed class ResumeEndpoint(
    IQuiescenceSignal signal,
    IIngressSourceRegistry registry,
    INotificationSender mediator)
    : ElsaEndpoint<ResumeRequest, StatusResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/resume");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(ResumeRequest req, CancellationToken ct)
    {
        var requestedBy = HttpContext.User.Identity?.Name;
        var before = signal.CurrentState;

        if ((before.Reason & QuiescenceReason.Drain) != 0)
        {
            await Send.ResponseAsync(StatusResponseFactory.Build(signal, registry), 409, ct);
            return;
        }

        var after = await signal.ResumeAsync(requestedBy, ct);

        // Audit only effective transitions (SC-007).
        var wasPaused = (before.Reason & QuiescenceReason.AdministrativePause) != 0;
        var isPaused = (after.Reason & QuiescenceReason.AdministrativePause) != 0;
        if (wasPaused && !isPaused)
            await mediator.SendAsync(new RuntimeResumeRequested(requestedBy, DateTimeOffset.UtcNow), ct);

        await Send.OkAsync(StatusResponseFactory.Build(signal, registry), ct);
    }
}
