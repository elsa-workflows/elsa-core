using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

internal class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? InstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
}

internal record Response(string WorkflowInstanceId);