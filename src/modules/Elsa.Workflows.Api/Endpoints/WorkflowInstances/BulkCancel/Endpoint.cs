using System.Text.Json.Serialization;
using Elsa.Abstractions;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

public class Endpoint : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/bulk-actions/cancel/workflow-instances/by-id");
        ConfigurePermissions("cancel:workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        // TODO: Implement workflow cancellation.
        var count = -1;

        return new(count);
    }

    public record BulkCancelWorkflowInstancesRequest(ICollection<string> Ids);

    public record BulkCancelWorkflowInstancesResponse([property: JsonPropertyName("cancelled")] int CancelledCount);
}