using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Resume;

/// <summary>
/// <c>POST /admin/workflow-runtime/resume</c> — clears <see cref="QuiescenceReason.AdministrativePause"/>.
/// Returns 409 Conflict when a drain is in progress (resume is meaningless mid-drain — see edge case in spec).
/// Idempotent on the success path.
/// </summary>
[PublicAPI]
internal sealed class ResumeEndpoint(IWorkflowRuntimeAdminService admin) : ElsaEndpoint<ResumeRequest, StatusResponse>
{
    public override void Configure()
    {
        Post("/admin/workflow-runtime/resume");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(ResumeRequest req, CancellationToken ct)
    {
        var status = admin.GetStatus();
        if ((status.State.Reason & QuiescenceReason.Drain) != 0)
        {
            await Send.ResponseAsync(StatusResponseFactory.Build(status), 409, ct);
            return;
        }

        await admin.ResumeAsync(HttpContext.User.Identity?.Name, ct);
        await Send.OkAsync(StatusResponseFactory.Build(admin.GetStatus()), ct);
    }
}
