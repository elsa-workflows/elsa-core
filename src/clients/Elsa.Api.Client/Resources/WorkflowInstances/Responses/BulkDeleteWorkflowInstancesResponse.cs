using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Responses;

internal sealed class BulkDeleteWorkflowInstancesResponse
{
    [JsonPropertyName("deleted")] public int DeletedCount { get; }
}