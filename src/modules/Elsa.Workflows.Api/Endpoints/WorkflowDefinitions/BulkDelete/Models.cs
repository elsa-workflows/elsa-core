using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.BulkDelete;

public class Request
{
    public ICollection<string> DefinitionIds { get; set; } = default!;
}

public class Response
{
    public Response(int deletedCount)
    {
        DeletedCount = deletedCount;
    }

    [JsonPropertyName("deleted")] public int DeletedCount { get; }
}