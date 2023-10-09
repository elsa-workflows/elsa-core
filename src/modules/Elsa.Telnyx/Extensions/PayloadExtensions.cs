using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Extensions;

/// <summary>
/// Provides extensions on <see cref="Payload"/>.
/// </summary>
public static class PayloadExtensions
{
    /// <summary>
    /// Extracts a <see cref="ClientStatePayload"/> from the specified <see cref="Payload"/>. 
    /// </summary>
    public static ClientStatePayload? GetClientStatePayload(this Payload payload)
    {
        return !string.IsNullOrWhiteSpace(payload.ClientState) 
            ? ClientStatePayload.FromBase64(payload.ClientState) 
            : default;
    }
}