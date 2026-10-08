using Elsa.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Selectors;

/// <summary>
/// Returns the correlation ID from the <c>correlationId</c> query string parameter.
/// </summary>
public class QueryStringHttpCorrelationIdSelector : HttpCorrelationIdSelectorBase
{
    /// <inheritdoc />
    protected override string? GetCorrelationId(HttpContext httpContext)
    {
        return httpContext.Request.Query["correlationId"].FirstOrDefault();
    }
}