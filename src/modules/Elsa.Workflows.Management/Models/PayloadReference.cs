using System.Text.Json.Serialization;

namespace Elsa.Workflows.Management.Models;

public sealed record PayloadReference(
    [property: JsonRequired] string Url,
    [property: JsonRequired] string Type
)
{
    private PayloadReference() : this(string.Empty, string.Empty) { }
}
