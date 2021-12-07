using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class WorkerBase : IAsyncDisposable
    {
        protected readonly IClient Client;
        protected readonly ILogger Logger;
        protected readonly Func<IClient, Task> DisposeReceiverAction;

        public WorkerBase(
            IClient client,
            Func<IClient, Task> disposeReceiverAction,
            ILogger<WorkerBase> logger)
        {
            Client = client;
            DisposeReceiverAction = disposeReceiverAction;
            Logger = logger;
        }

        public async ValueTask DisposeAsync() => await DisposeReceiverAction(Client);
    }
}