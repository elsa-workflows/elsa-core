using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

internal class Request
{
    public ICollection<string>? Ids { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public ICollection<string>? WorkflowDefinitionIds { get; set; }
}

internal class Response(long deletedCount)
{
    [JsonPropertyName("deleted")] public long DeletedCount { get; } = deletedCount;
}