using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

internal class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? InstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }
    
    public VersionOptions? VersionOptions { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }

    public string? Channel { get; set; }
}

internal record Response(string WorkflowInstanceId);