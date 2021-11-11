using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class WorkerBase : IAsyncDisposable
    {
        protected readonly IClient _client;
        protected readonly ILogger _logger;
        protected readonly Func<IClient, Task> _disposeReceiverAction;

        public WorkerBase(
            IClient client,
            Func<IClient, Task> disposeReceiverAction,
            ILogger<WorkerBase> logger)
        {
            _client = client;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;
        }

        public async ValueTask DisposeAsync() => await _disposeReceiverAction(_client);
    }
}