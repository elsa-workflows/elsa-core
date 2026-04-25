using Elsa.Abstractions;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Admin.Endpoints.Admin.Status;

/// <summary>
/// <c>GET /admin/workflow-runtime/status</c> — returns the composite quiescence state, per-ingress-source state,
/// and active-burst count. Always readable (subject to authorisation) — even during drain.
/// </summary>
[PublicAPI]
internal sealed class StatusEndpoint(IQuiescenceSignal signal, IIngressSourceRegistry registry)
    : ElsaEndpointWithoutRequest<StatusResponse>
{
    public override void Configure()
    {
        Get("/admin/workflow-runtime/status");
        ConfigurePermissions(PermissionNames.ManageWorkflowRuntime);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(StatusResponseFactory.Build(signal, registry), ct);
    }
}
