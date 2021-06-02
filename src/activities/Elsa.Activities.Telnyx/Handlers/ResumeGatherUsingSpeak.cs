using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Handlers
{
    public class ResumeGatherUsingSpeak : ResumeWebhookDrivenActivity<GatherUsingSpeak, CallGatherEndedPayload>
    {
        public ResumeGatherUsingSpeak(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }
    }
}