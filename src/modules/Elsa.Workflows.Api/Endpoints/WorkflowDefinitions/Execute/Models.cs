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
    string? Name { get; }
    string? TriggerActivityId { get; }
    ActivityHandle? ActivityHandle { get; }
    VersionOptions? VersionOptions { get; }

    IDictionary<string, object>? GetInputAsDictionary();
    IDictionary<string, object>? GetVariablesAsDictionary();
}

public class PostRequest : IExecutionRequest
{
    public string DefinitionId { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }
    public string? TriggerActivityId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public VersionOptions? VersionOptions { get; set; }

    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Input { get; set; }
    
    [JsonConverter(typeof(ExpandoObjectConverterFactory))]
    public object? Variables { get; set; }

    public IDictionary<string, object>? GetInputAsDictionary() => (IDictionary<string, object>?)Input;
    public IDictionary<string, object>? GetVariablesAsDictionary() => (IDictionary<string, object>?)Variables;
}

public class GetRequest : IExecutionRequest
{
    public string DefinitionId { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }
    public string? TriggerActivityId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public VersionOptions? VersionOptions { get; set; }
    public string? Input { get; set; }
    public string? Variables { get; set; }

    public IDictionary<string, object>? GetInputAsDictionary() => ParseStringAsDictionary(Input);
    public IDictionary<string, object>? GetVariablesAsDictionary() => ParseStringAsDictionary(Variables);

    private IDictionary<string, object>? ParseStringAsDictionary(string? value)
    {
        var result = value?.TryConvertTo<ExpandoObject>(new()
        {
            SerializerOptions = new()
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