using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

internal class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }

    [Obsolete("Use VersionOptions instead.")]
    public int? Version { get; set; }

    public VersionOptions? VersionOptions { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
}

internal class Response
{
    public Response(WorkflowState workflowState)
    {
        WorkflowState = workflowState;
    }

    public WorkflowState WorkflowState { get; }
}