using Elsa.Activities.RabbitMq.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IRabbitMqQueueStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
        Task<ReceiverWorker> CreateReceiverWorkerAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
        Task<SenderWorker> CreateSenderWorkerAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default);
    }
}