using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.MassTransit.Messages;

// ReSharper disable once InconsistentNaming
public interface DispatchResumeWorkflows
{
    string ActivityTypeName { get; set; }

    [JsonConverter(typeof(PolymorphicConverter))]
    object BookmarkPayload { get; set; }

    string? CorrelationId { get; set; }
    IDictionary<string, object>? Input { get; set; }
}