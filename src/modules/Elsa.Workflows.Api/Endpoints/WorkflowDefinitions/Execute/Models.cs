using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public interface IExecutionRequest
{
    string DefinitionId { get; }
    string? CorrelationId { get; }
    string? TriggerActivityId { get; }
    ActivityHandle? ActivityHandle { get; }
    VersionOptions? VersionOptions { get; }

    IDictionary<string, object>? GetInputAsDictionary();
}

public class PostRequest : IExecutionRequest
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public VersionOptions? VersionOptions { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }

    public IDictionary<string, object>? GetInputAsDictionary() => (IDictionary<string, object>?)Input;
}

public class GetRequest : IExecutionRequest
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public string? TriggerActivityId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public VersionOptions? VersionOptions { get; set; }
    public string? Input { get; set; }

    public IDictionary<string, object>? GetInputAsDictionary()
    {
        var result = Input?.TryConvertTo<ExpandoObject>(new ObjectConverterOptions
        {
            SerializerOptions = new JsonSerializerOptions
            {
                Converters = { new ExpandoObjectConverter() }
            }
        });

        return result?.Success == true ? (IDictionary<string, object>?)result.Value : null;
    }
}

public class Response(WorkflowState workflowState)
{
    public WorkflowState WorkflowState { get; } = workflowState;
}