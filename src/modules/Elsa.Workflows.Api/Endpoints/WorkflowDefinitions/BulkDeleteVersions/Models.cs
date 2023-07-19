using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDeleteVersions;

internal class Request
{
    public ICollection<string> Ids { get; set; } = default!;
}

internal class Response
{
    public Response(long deletedCount)
    {
        DeletedCount = deletedCount;
    }

    [JsonPropertyName("deleted")] public long DeletedCount { get; }
}