using Elsa.Abstractions;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.RuntimeAdmin.Status;

/// <summary>
/// <c>GET /admin/workflow-runtime/status</c> — returns the composite quiescence state, per-ingress-source state,
/// and active-execution cycle count. Always readable (subject to authorisation) — even during drain.
/// </summary>
[PublicAPI]
internal sealed class StatusEndpoint(IWorkflowRuntimeAdminService admin) : ElsaEndpointWithoutRequest<StatusResponse>
{
    public override void Configure()
    {
        Get("/admin/workflow-runtime/status");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(StatusResponseFactory.Build(admin.GetStatus()), ct);
    }
}
