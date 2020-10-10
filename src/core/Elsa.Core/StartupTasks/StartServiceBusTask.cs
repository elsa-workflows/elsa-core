using System.Threading;
using System.Threading.Tasks;
using Elsa.Messaging.Distributed;
using Elsa.Runtime;
using Rebus.Bus;

namespace Elsa.StartupTasks
{
    public class StartServiceBusTask : IStartupTask
    {
        private readonly IBus _serviceBus;
        public StartServiceBusTask(IBus serviceBus) => _serviceBus = serviceBus;
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => _serviceBus.Subscribe<RunWorkflow>();
    }
}