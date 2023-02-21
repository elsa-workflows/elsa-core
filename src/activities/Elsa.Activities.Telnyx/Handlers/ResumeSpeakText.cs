using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeSpeakText : ResumeWebhookDrivenActivity<SpeakText, CallSpeakEnded>
    {
        public ResumeSpeakText(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }

        protected override IBookmark CreateBookmark(CallPayload receivedPayload)
        {
            return new SpeakTextBookmark
            {
                CallControlId = receivedPayload.CallControlId
            };
        }
    }
}