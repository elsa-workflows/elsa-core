using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.AzureServiceBus.Services
{
    public interface IServiceBusConsumer
    {
        void RegisterOnMessageHandlerAndReceiveMessages();
        Task CloseQueueAsync();
    }
}
