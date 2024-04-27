using System.Text.Json.Serialization;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

[Obsolete("This message is no longer used and will be removed in a future version.")]
public class DispatchResumeWorkflows(string activityTypeName, object stimulus)
{
    public string ActivityTypeName { get; init; } = activityTypeName;

    [JsonConverter(typeof(PolymorphicObjectConverterFactory))]
    [Obsolete("Use Stimulus instead.")]
    public object? BookmarkPayload { get; init; }
    
    [JsonConverter(typeof(PolymorphicObjectConverterFactory))]
    public object? Stimulus { get; init; } = stimulus;

    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
}