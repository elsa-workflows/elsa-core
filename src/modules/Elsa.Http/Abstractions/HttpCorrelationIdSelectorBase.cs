using Elsa.Http.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Abstractions;

/// <summary>
/// Provides a base class for implementing <see cref="IHttpCorrelationIdSelector"/>.
/// </summary>
public abstract class HttpCorrelationIdSelectorBase : IHttpCorrelationIdSelector
{
    /// <inheritdoc />
    public virtual double Priority => 0;

    /// <summary>
    /// Override this method to return the correlation ID for the specified HTTP context, or <c>null</c> if no correlation ID could be found.
    /// </summary>
    protected virtual ValueTask<string?> GetCorrelationIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId(httpContext);
        return new(correlationId);
    }

    /// <summary>
    /// Override this method to return the correlation ID for the specified HTTP context, or <c>null</c> if no correlation ID could be found.
    /// </summary>
    protected virtual string? GetCorrelationId(HttpContext httpContext) => null;

    ValueTask<string?> IHttpCorrelationIdSelector.GetCorrelationIdAsync(HttpContext httpContext, CancellationToken cancellationToken) => GetCorrelationIdAsync(httpContext, cancellationToken);
}