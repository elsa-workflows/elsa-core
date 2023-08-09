using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

public record DispatchResumeWorkflows(
    string ActivityTypeName,
    [property: JsonConverter(typeof(PolymorphicObjectConverterFactory))]
    object BookmarkPayload,
    string? CorrelationId,
    string? WorkflowInstanceId,
    string? ActivityInstanceId,
    IDictionary<string, object>? Input
);