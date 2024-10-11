using System.Text.Json.Serialization;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

[Obsolete("This message is no longer used and will be removed in a future version.")]
public class DispatchResumeWorkflows(string activityTypeName, object stimulus)
{
    public string ActivityTypeName { get; init; } = activityTypeName;

    [JsonConverter(typeof(PolymorphicObjectConverterFactory))]
    public object BookmarkPayload { get; init; } = stimulus;

    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }

    [Obsolete("This property is no longer used and will be removed in a future version. Use the SerializedInput property instead.")]
    public IDictionary<string, object>? Input { get; set; }

    public string? SerializedInput { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
}