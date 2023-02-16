using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

public record DispatchTriggerWorkflows
(
    string ActivityTypeName,

    [property: JsonConverter(typeof(PolymorphicConverter))]
    object BookmarkPayload,

    string? CorrelationId,
    IDictionary<string, object>? Input
);