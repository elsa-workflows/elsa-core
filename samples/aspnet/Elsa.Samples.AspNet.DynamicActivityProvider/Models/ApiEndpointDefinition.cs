using System.Text.Json.Serialization;

namespace Elsa.Samples.AspNet.DynamicActivityProvider.Models;

public record ApiEndpointDefinition(
    string Name,
    string Description,
    string Path,
    string Method,
    [property: JsonPropertyName("params")] ICollection<ApiParameterDefinition> Parameters);