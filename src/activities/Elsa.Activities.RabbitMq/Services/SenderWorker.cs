using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class SenderWorker : WorkerBase
    {
        public SenderWorker(
            IClient receiverClient,
            Func<IClient, Task> disposeReceiverAction,
            ILogger<SenderWorker> logger): base (receiverClient, disposeReceiverAction, logger)
        {
            Client.StartAsOneWayClient();
        }
    }
}