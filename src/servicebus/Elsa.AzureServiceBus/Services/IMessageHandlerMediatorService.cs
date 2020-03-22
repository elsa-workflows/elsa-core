using Elsa.AzureServiceBus.Notifications;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.AzureServiceBus.Services
{
    public interface IMessageHandlerMediatorService
    {
        Task Process(ProcessMessageNotification message, CancellationToken token);
    }
}
