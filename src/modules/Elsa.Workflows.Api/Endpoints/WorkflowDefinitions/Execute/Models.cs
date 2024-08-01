using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public interface IExecutionRequest
{
    string DefinitionId { get; }
    string? CorrelationId { get; }
    string? TriggerActivityId { get; }
    VersionOptions? VersionOptions { get; }
}

public class PostRequest : IExecutionRequest
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }
    public VersionOptions? VersionOptions { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
}

public class GetRequest : IExecutionRequest
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }
    public VersionOptions? VersionOptions { get; set; }
    public string? Input { get; set; }
}

public class Response(WorkflowState workflowState)
{
    public WorkflowState WorkflowState { get; } = workflowState;
}