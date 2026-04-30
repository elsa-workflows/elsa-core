using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

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
            // Use the discriminated ConflictResponse shape to match ForceDrain's 409 contract; routed via
            // HttpContext.Response.WriteAsJsonAsync because Send.ResponseAsync is constrained to TResponse.
            HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await HttpContext.Response.WriteAsJsonAsync(
                new ConflictResponse { Code = "runtime-draining", State = StatusResponseFactory.Build(status) },
                ct);
            return;
        }

        await admin.ResumeAsync(HttpContext.User.Identity?.Name, ct);
        await Send.OkAsync(StatusResponseFactory.Build(admin.GetStatus()), ct);
    }
}
