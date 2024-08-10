using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Refresh;

internal class Request
{
    public ICollection<string>? DefinitionIds { get; set; }
}

internal class Response(ICollection<string> refreshed, ICollection<string> notFound)
{
    [JsonPropertyName("refreshed")] public ICollection<string> Refreshed { get; } = refreshed;
    [JsonPropertyName("notFound")] public ICollection<string> NotFound { get; } = notFound;
}