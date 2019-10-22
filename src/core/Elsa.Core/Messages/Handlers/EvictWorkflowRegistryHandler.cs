using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Services;
using MediatR;

namespace Elsa.Messages.Handlers
{
    public class EvictWorkflowRegistryHandler : INotificationHandler<WorkflowDefinitionStoreUpdated>
    {
        private readonly ISignal signal;

        public EvictWorkflowRegistryHandler(ISignal signal)
        {
            this.signal = signal;
        }
        
        public Task Handle(WorkflowDefinitionStoreUpdated notification, CancellationToken cancellationToken)
        {
            signal.Trigger(WorkflowRegistry.CacheKey);
            return Task.CompletedTask;
        }
    }
}