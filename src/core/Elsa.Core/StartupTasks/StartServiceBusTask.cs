using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages.Distributed;
using Elsa.Runtime;
using Rebus.Bus;

namespace Elsa.StartupTasks
{
    public class StartServiceBusTask : IStartupTask
    {
        private readonly IBus serviceBus;
        public StartServiceBusTask(IBus serviceBus) => this.serviceBus = serviceBus;
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => serviceBus.Subscribe<RunWorkflow>();
    }
}