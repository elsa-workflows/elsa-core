using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.State;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public abstract class RequestBase
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }

    public VersionOptions? VersionOptions { get; set; }
}

public class PostRequest : RequestBase
{
    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
}

public class GetRequest : RequestBase
{
    [QueryParam]
    public string? Input { get; set; }
}

public class Response(WorkflowState workflowState)
{
    public WorkflowState WorkflowState { get; } = workflowState;
}