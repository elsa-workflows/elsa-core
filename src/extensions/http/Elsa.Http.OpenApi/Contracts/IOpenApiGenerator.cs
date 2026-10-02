using Elsa.Http.OpenApi.Models;

namespace Elsa.Http.OpenApi.Contracts;

/// <summary>
/// Contract for generating OpenAPI JSON documentation.
/// </summary>
public interface IOpenApiGenerator
{
    /// <summary>
    /// Generates OpenAPI JSON documentation from a list of endpoint definitions.
    /// </summary>
    /// <param name="endpoints">The list of endpoint definitions.</param>
    /// <returns>OpenAPI JSON string.</returns>
    string GenerateOpenApiJson(List<EndpointDefinition> endpoints);
}
