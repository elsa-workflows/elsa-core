using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

internal class Request
{
    public ICollection<string> Ids { get; set; } = default!;
}

internal class Response
{
    public Response(int deletedCount)
    {
        DeletedCount = deletedCount;
    }

    [JsonPropertyName("deleted")] public int DeletedCount { get; }
}