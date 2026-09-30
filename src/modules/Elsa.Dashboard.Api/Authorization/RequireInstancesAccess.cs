using FastEndpoints;

namespace Elsa.Dashboard.Api.Authorization;

/// <summary>
/// Answers 403 unless the caller holds <c>dashboard:view</c> or <c>workflows/instances:view</c>. The single-purpose
/// dashboard endpoints (trends, recent activity, needs attention, hotspots) all serve workflow instance data.
/// </summary>
/// <remarks>
/// This deviates from <c>RequirePermission</c> on purpose: that helper requires one permission, and
/// <c>EndpointPermissionRegistry</c> records exactly one permission per endpoint type (a public
/// <c>IReadOnlyDictionary&lt;Type, Permission&gt;</c> its tooling and tests consume), so an "either of two" requirement
/// cannot be recorded there without changing that contract. The guard is therefore invisible to registry-based tooling.
/// </remarks>
internal sealed class RequireInstancesAccess<TRequest> : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken cancellationToken)
    {
        if (!DashboardAccess.CanReadInstances(context.HttpContext))
        {
            await context.HttpContext.Response.SendForbiddenAsync(cancellationToken);
        }
    }
}
