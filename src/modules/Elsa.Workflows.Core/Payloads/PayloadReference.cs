using System.Text.Json.Serialization;

namespace Elsa.Workflows.Payloads;

public sealed record PayloadReference(
    [property: JsonRequired] string Url,
    [property: JsonRequired] string TypeIdentifier,
    string? CompressionAlgorithm
)
{
    private PayloadReference() : this(string.Empty, string.Empty, null) { }

    public bool IsValid => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(TypeIdentifier);
}
