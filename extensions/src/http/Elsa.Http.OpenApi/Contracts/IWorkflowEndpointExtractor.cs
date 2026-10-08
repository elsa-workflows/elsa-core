using Elsa.Http.OpenApi.Models;

namespace Elsa.Http.OpenApi.Contracts;

/// <summary>
/// Contract for extracting workflow HTTP endpoints for OpenAPI documentation.
/// </summary>
public interface IWorkflowEndpointExtractor
{
    /// <summary>
    /// Extracts all HTTP endpoints from workflows.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of endpoint definitions.</returns>
    Task<List<EndpointDefinition>> ExtractEndpointsAsync(CancellationToken cancellationToken = default);
}
