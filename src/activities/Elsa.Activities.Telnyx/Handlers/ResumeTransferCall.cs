using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeTransferCall : ResumeWebhookDrivenActivity<TransferCall>
    {
        public ResumeTransferCall(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }
        
        protected override IEnumerable<Type> GetSupportedPayloadTypes() => new[] {typeof(CallAnsweredPayload), typeof(CallHangupPayload)};
        protected override IBookmark CreateBookmark(CallPayload receivedPayload)
        {
            return new TransferCallBookmark
            {
                CallControlId = receivedPayload.CallControlId
            };
        }
    }
}