using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeStopAudioPlayback : ResumeWebhookDrivenActivity<StopAudioPlayback, CallPlaybackEndedPayload>
    {
        public ResumeStopAudioPlayback(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }

        protected override IBookmark CreateBookmark(CallPayload receivedPayload)
        {
            return new StopAudioPlaybackBookmark
            {
                CallControlId = receivedPayload.CallControlId
            };
        }
    }
}