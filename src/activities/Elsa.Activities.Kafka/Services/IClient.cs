using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Activities.Kafka.Models;

namespace Elsa.Activities.Kafka.Services
{
    public interface IClient
    {
        KafkaConfiguration Configuration { get; }
        Task StartProcessing(string topic, string group);
        Task PublishMessage(string message);
        void SetHandlers(Func<KafkaMessageEvent, Task> receiveHandler, Func<Exception, Task> errorHandler, CancellationToken cancellationToken);
        Task Dispose();
    }
}