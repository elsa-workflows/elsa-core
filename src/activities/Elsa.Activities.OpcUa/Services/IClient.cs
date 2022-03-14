using Elsa.Activities.OpcUa.Configuration;
using Opc.Ua.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Services
{
    public interface IClient
    {
        OpcUaBusConfiguration Configuration { get; }
        void SubscribeWithHandler(Func<MonitoredItem, CancellationToken, Task> handler);
        Task PublishMessage(string message);
        void StartClient();
        void StopClient();
        void Dispose();
    }
}