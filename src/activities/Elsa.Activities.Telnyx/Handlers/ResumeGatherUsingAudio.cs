using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeGatherUsingAudio : ResumeWebhookDrivenActivity<GatherUsingAudio, CallGatherEndedPayload>
    {
        public ResumeGatherUsingAudio(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }
    }
}