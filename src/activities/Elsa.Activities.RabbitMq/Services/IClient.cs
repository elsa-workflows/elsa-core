using Elsa.Activities.RabbitMq.Configuration;
using Rebus.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public interface IClient
    {
        RabbitMqBusConfiguration Configuration { get; }
        void StartWithHandler(Func<TransportMessage, CancellationToken, Task> handler);
        void StartAsOneWayClient();
        Task PublishMessage(string message);
        void SetIsReceivingMessages(bool isReceivingMessages);
        void Dispose();
    }
}