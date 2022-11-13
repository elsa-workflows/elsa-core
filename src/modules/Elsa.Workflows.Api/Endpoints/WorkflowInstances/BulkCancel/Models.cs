using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

public class Request
{
    public ICollection<string> Ids { get; set; } = default!;
}

public class Response
{
    public Response(int cancelledCount)
    {
        CancelledCount = cancelledCount;
    }

    [JsonPropertyName("cancelled")] public int CancelledCount { get; }
}