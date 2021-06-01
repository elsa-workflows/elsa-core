using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Bookmarks;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Activities.Telnyx.Webhooks.Consumers
{
    internal abstract class TriggerBlockingCallActivities<TPayload, TActivity> : IHandleMessages<TPayload> where TPayload : CallPayload where TActivity : IActivity
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        protected TriggerBlockingCallActivities(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        public async Task Handle(TPayload message)
        {
            var correlationId = GetCorrelationId(message);
            var trigger = CreateBookmark();
            var bookmark = CreateBookmark();
            var context = new CollectWorkflowsContext(typeof(TActivity).Name, bookmark, trigger, correlationId);
            await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, message);
        }

        protected abstract IBookmark CreateBookmark();

        private string GetCorrelationId(TPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.ClientState))
            {
                var clientStatePayload = ClientStatePayload.FromBase64(payload.ClientState);
                return clientStatePayload.CorrelationId;
            }

            return payload.CallSessionId;
        }
    }

    internal abstract class TriggerBlockingCallActivities<TPayload, TActivity, TBookmark> : TriggerBlockingCallActivities<TPayload, TActivity> where TPayload : CallPayload where TActivity : IActivity where TBookmark : class, IBookmark, new()
    {
        protected TriggerBlockingCallActivities(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }

        protected override IBookmark CreateBookmark() => new TBookmark();
    }
}