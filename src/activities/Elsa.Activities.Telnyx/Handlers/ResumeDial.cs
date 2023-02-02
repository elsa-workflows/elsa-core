using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeDial : ResumeWebhookDrivenActivity<Dial>
    {
        public ResumeDial(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }

        protected override IEnumerable<Type> GetSupportedPayloadTypes() => new[]
            { typeof(CallInitiatedPayload), typeof(CallAnsweredPayload), typeof(CallHangupPayload), typeof(CallMachinePremiumDetectionEnded), typeof(CallMachineGreetingEnded), typeof(CallMachinePremiumGreetingEnded) };

        protected override IBookmark CreateBookmark(CallPayload receivedPayload)
        {
            return new DialBookmark
            {
                CallControlId = receivedPayload.CallControlId
            };
        }
    }
}