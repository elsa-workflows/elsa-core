using Elsa.AzureServiceBus.Notifications;
using MediatR;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.AzureServiceBus.Services
{
    public class MessageHandlerMediatorService : IMessageHandlerMediatorService
    {
        private readonly IMediator _mediator;

        public MessageHandlerMediatorService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task Process(ProcessMessageNotification message, CancellationToken token)
        {
            return _mediator.Publish(message, token);
        }
    }
}
