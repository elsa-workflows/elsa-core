using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Responses;

internal class BulkDeleteWorkflowInstancesResponse
{
    [JsonPropertyName("deleted")] public int DeletedCount { get; }
}