using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Activities.Telnyx.Scripting.JavaScript
{
    public class TelnyxTypeDefinitionProvider : TypeDefinitionProvider
    {
        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context)
        {
            return new[]
            {
                typeof(TelnyxWebhook),
                typeof(Extension),
                
                typeof(CallAnsweredPayload),
                typeof(CallBridgedPayload),
                typeof(CallDtmfReceivedPayload),
                typeof(CallGatherEndedPayload),
                typeof(CallHangupPayload),
                typeof(CallInitiatedPayload),
                typeof(CallPayload),
                typeof(CallPlayback),
                typeof(CallPlaybackEndedPayload),
                typeof(CallPlaybackStartedPayload),
                typeof(CallRecordingSaved),
                typeof(CallRecordingUrls),
                typeof(CallSpeakEnded),
                typeof(CallSpeakStarted),
            };
        }
    }
}