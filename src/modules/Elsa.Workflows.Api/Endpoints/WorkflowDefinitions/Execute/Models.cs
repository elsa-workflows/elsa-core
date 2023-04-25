using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    
    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
}

public class Response
{
    public Response(WorkflowState workflowState)
    {
        WorkflowState = workflowState;
    }
    
    public WorkflowState WorkflowState { get; }
}