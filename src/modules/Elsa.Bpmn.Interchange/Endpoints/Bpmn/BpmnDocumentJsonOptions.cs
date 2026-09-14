using System.Text.Json;
using Bpmn.Model;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The <see cref="JsonSerializerOptions"/> the <c>bpmn/definitions/{definitionId}/document</c> endpoints read and
/// write a <see cref="BpmnDefinitions"/> document with.
/// </summary>
/// <remarks>
/// <c>Bpmn.Model</c> declares every serialized property name explicitly through <c>[JsonPropertyName]</c> — see
/// <see cref="BpmnPayloadFormat"/> — and carries no <c>[JsonConverter]</c> of its own, so its wire shape is
/// plain <see cref="System.Text.Json.JsonSerializer"/> defaults: camelCase names exactly as declared, and any enum
/// as its underlying integer. Elsa's own API-wide serializer (<c>Elsa.Workflows.Serialization.Serializers.ApiSerializer</c>,
/// reached through FastEndpoints' configured <c>IApiSerializer</c>) adds a <c>JsonStringEnumConverter</c> and several
/// other converters of its own, none of which this library's schema expects. Letting the document body go through
/// that serializer, the way an ordinary FastEndpoints request or response DTO does, would silently write a shape
/// <c>Bpmn.Model</c>'s own reader does not agree is the payload format it published — hence the document endpoints
/// binding and writing the body explicitly with this instance instead of a request/response DTO.
/// </remarks>
internal static class BpmnDocumentJsonOptions
{
    /// <summary>Plain <see cref="System.Text.Json"/> defaults, scoped to the document endpoints only.</summary>
    public static readonly JsonSerializerOptions Value = new();
}
