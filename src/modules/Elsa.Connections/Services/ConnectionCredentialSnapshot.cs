using System.Text.Json.Serialization;
using Elsa.Connections.Models;

namespace Elsa.Connections.Services;

// Private consumers can revalidate the published generation without resolving its bearer again.
internal sealed record ConnectionCredentialSnapshot(
    [property: JsonIgnore] ConnectionAccessCredential Credential,
    [property: JsonIgnore] long Revision,
    [property: JsonIgnore] string GenerationId,
    [property: JsonIgnore] string SecretName)
{
    public override string ToString() => "ConnectionCredentialSnapshot { Redacted = true }";
}
