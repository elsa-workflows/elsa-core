using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Activities.Telnyx.Webhooks.Handlers
{
    internal class SendTelnyxWebhookReceivedMessage : INotificationHandler<TelnyxWebhookReceived>
    {
        private readonly ICommandSender _commandSender;
        public SendTelnyxWebhookReceivedMessage(ICommandSender commandSender) => _commandSender = commandSender;
        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken) => await _commandSender.SendAsync(notification);
    }
}