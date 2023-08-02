using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

public record DispatchTriggerWorkflows
(
    string ActivityTypeName,
    [property: JsonConverter(typeof(PolymorphicObjectConverterFactory))]
    object BookmarkPayload,
    string? CorrelationId,
    string? WorkflowInstanceId,
    IDictionary<string, object>? Input
);