using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Contracts;

/// <summary>
/// Provides a way to select the correlation ID for a request.
/// </summary>
public interface IHttpCorrelationIdSelector
{
    /// <summary>
    /// The priority of this selector. The selector with the highest priority will be used.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// Returns the correlation ID for the specified HTTP context, or <c>null</c> if no correlation ID could be found.
    /// </summary>
    ValueTask<string?> GetCorrelationIdAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}