using System.Text.Json.Serialization;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

[Obsolete("This is no longer used and will be removed in a future version.")]
public class DispatchTriggerWorkflows(string activityTypeName, object bookmarkPayload)
{
    public string ActivityTypeName { get; init; } = activityTypeName;

    [JsonConverter(typeof(PolymorphicObjectConverterFactory))]
    public object BookmarkPayload { get; init; } = bookmarkPayload;

    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
}