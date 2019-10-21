using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using MediatR;

namespace Elsa.Messages.Handlers
{
    public class EvictWorkflowRegistryHandler : INotificationHandler<WorkflowDefinitionStoreUpdated>
    {
        private readonly IWorkflowRegistry workflowRegistry;

        public EvictWorkflowRegistryHandler(IWorkflowRegistry workflowRegistry)
        {
            this.workflowRegistry = workflowRegistry;
        }
        
        public async Task Handle(WorkflowDefinitionStoreUpdated notification, CancellationToken cancellationToken)
        {
            await workflowRegistry.EvictAsync(cancellationToken);
        }
    }
}