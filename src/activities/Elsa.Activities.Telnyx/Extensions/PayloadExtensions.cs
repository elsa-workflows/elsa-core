using System;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;

namespace Elsa.Activities.Telnyx.Extensions;

public static class PayloadExtensions
{
    public static string? GetCorrelationId(this Payload payload)
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
}