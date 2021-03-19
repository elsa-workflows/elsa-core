using System;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    public abstract record CallPlayback : CallPayload
    {
        public Uri MediaUrl { get; init; } = default!;
        public bool Overlay { get; set; }
    }
}