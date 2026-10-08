using Elsa.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Selectors;

/// <summary>
/// Returns the correlation ID from the <c>X-Correlation-ID</c> header, if any.
/// </summary>
public class HeaderHttpCorrelationIdSelector : HttpCorrelationIdSelectorBase
{
    /// <inheritdoc />
    protected override string? GetCorrelationId(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    }
}