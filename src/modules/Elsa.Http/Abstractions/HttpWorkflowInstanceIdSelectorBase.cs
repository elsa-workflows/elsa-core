using Elsa.Http.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Abstractions;

/// <summary>
/// Provides a base class for implementing <see cref="IHttpCorrelationIdSelector"/>.
/// </summary>
public abstract class HttpWorkflowInstanceIdSelectorBase : IHttpWorkflowInstanceIdSelector
{
    /// <inheritdoc />
    public virtual double Priority => 0;

    /// <summary>
    /// Override this method to return the workflow instance ID for the specified HTTP context, or <c>null</c> if no workflow instance ID could be found.
    /// </summary>
    protected virtual ValueTask<string?> GetWorkflowInstanceIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var workflowInstanceId = GetWorkflowInstanceId(httpContext);
        return new(workflowInstanceId);
    }

    /// <summary>
    /// Override this method to return the workflow instance ID for the specified HTTP context, or <c>null</c> if no workflow instance ID could be found.
    /// </summary>
    protected virtual string? GetWorkflowInstanceId(HttpContext httpContext) => null;

    ValueTask<string?> IHttpWorkflowInstanceIdSelector.GetWorkflowInstanceIdAsync(HttpContext httpContext, CancellationToken cancellationToken) => GetWorkflowInstanceIdAsync(httpContext, cancellationToken);
}