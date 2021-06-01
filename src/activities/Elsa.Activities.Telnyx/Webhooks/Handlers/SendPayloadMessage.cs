using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Services;
using MediatR;

namespace Elsa.Activities.Telnyx.Webhooks.Handlers
{
    internal abstract class SendPayloadMessage<TPayload> : INotificationHandler<TelnyxWebhookReceived> where TPayload: Payload
    {
        private readonly ICommandSender _commandSender;
        protected SendPayloadMessage(ICommandSender commandSender) => _commandSender = commandSender;
        
        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            if (notification.Webhook.Data.Payload is not TPayload payload)
                return;

            await _commandSender.SendAsync(payload);
        }
    }
}