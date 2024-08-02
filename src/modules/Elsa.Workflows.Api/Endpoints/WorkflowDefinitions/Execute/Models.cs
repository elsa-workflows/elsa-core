using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }

    public VersionOptions? VersionOptions { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
}

public class Response(WorkflowState workflowState)
{
    public WorkflowState WorkflowState { get; } = workflowState;
}