using Elsa.Activities.Telnyx.Webhooks.Handlers;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Telnyx.Activities
{
    internal class CallGatherEndedHandler : SendPayloadMessage<CallGatherEndedPayload>
    {
        public CallGatherEndedHandler(ICommandSender commandSender) : base(commandSender)
        {
        }
    }
}