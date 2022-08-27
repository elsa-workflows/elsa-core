using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkDelete;

public class Request
{
    public ICollection<string> Ids { get; set; } = default!;
}

public class Response
{
    public Response(int deletedCount)
    {
        DeletedCount = deletedCount;
    }

    [JsonPropertyName("deleted")] public int DeletedCount { get; }
}