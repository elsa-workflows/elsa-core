using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Pause;

/// <summary>
/// <c>POST /admin/workflow-runtime/pause</c> — places the runtime into <see cref="QuiescenceReason.AdministrativePause"/>.
/// Idempotent: subsequent calls return the current state without producing additional audit events (SC-007).
/// </summary>
[PublicAPI]
internal sealed class PauseEndpoint(IWorkflowRuntimeAdminService admin) : ElsaEndpoint<PauseRequest, StatusResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/pause");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(PauseRequest req, CancellationToken ct)
    {
        await admin.PauseAsync(req.Reason, HttpContext.User.Identity?.Name, ct);
        await Send.OkAsync(StatusResponseFactory.Build(admin.GetStatus()), ct);
    }
}
