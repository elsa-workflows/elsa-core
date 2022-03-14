using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.OpcUa.Services;
using Elsa.Services;

namespace Elsa.Activities.OpcUa.StartupTasks
{
    public class StartOpcUaQueues : IStartupTask
    {
        private readonly IOpcUaQueueStarter _OpcUaQueueStarter;
        public StartOpcUaQueues(IOpcUaQueueStarter OpcUaQueueStarter) => _OpcUaQueueStarter = OpcUaQueueStarter;
        public int Order => 1000;
        public Task ExecuteAsync(CancellationToken stoppingToken) => _OpcUaQueueStarter.CreateWorkersAsync(stoppingToken);
    }
}