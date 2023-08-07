using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Payloads.Call;

namespace Elsa.Telnyx.Extensions;

/// <summary>
/// Provides extensions on <see cref="Payload"/>.
/// </summary>
public static class PayloadExtensions
{
    /// <summary>
    /// Extracts a correlation ID from the specified <see cref="Payload"/>. If the payload carries client state, the correlation ID is taken from there.
    /// Otherwise, if the payload is of type <see cref="CallPayload"/>, its <see cref="CallPayload.CallSessionId"/> is used. 
    /// </summary>
    public static string GetCorrelationId(this Payload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.ClientState))
        {
            var clientStatePayload = ClientStatePayload.FromBase64(payload.ClientState);
            return clientStatePayload.CorrelationId;
        }

        if (payload is CallPayload callPayload)
            return callPayload.CallSessionId;

        throw new NotSupportedException($"The received payload type {payload.GetType().Name} is not supported yet.");
    }
    
    /// <summary>
    /// Extracts a correlation ID from the specified <see cref="Payload"/>. If the payload carries client state, the correlation ID is taken from there.
    /// Otherwise, if the payload is of type <see cref="CallPayload"/>, its <see cref="CallPayload.CallSessionId"/> is used. 
    /// </summary>
    public static ClientStatePayload GetClientStatePayload(this Payload payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.ClientState))
            return ClientStatePayload.FromBase64(payload.ClientState);

        var correlationId = payload.GetCorrelationId();
        return new ClientStatePayload(correlationId);
    }
}