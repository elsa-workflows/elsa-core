using System;
using System.Collections.Generic;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeDial : ResumeWebhookDrivenActivity<Dial>
    {
        public ResumeDial(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }

        protected override IEnumerable<Type> GetSupportedPayloadTypes() => new[] {typeof(CallInitiatedPayload), typeof(CallAnsweredPayload), typeof(CallHangupPayload)};
    }
}