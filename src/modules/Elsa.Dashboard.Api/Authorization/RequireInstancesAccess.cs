using FastEndpoints;

namespace Elsa.Dashboard.Api.Authorization;

/// <summary>
/// Answers 403 unless the caller holds <c>dashboard:view</c> or <c>workflows/instances:view</c>. The single-purpose
/// dashboard endpoints (trends, recent activity, needs attention, hotspots) all serve workflow instance data.
/// </summary>
internal sealed class RequireInstancesAccess<TRequest> : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken cancellationToken)
    {
        if (!DashboardAccess.CanReadInstances(context.HttpContext) && !context.HttpContext.Response.HasStarted)
            await context.HttpContext.Response.SendForbiddenAsync(cancellationToken);
    }
}
