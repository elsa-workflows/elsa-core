using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Services;
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