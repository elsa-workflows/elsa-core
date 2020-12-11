using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using MediatR;

namespace Elsa.Services
{
    public class WorkflowInstanceManager : IWorkflowInstanceManager
    {
        private readonly IMediator _mediator;

        public WorkflowInstanceManager(IWorkflowInstanceStore store, IMediator mediator)
        {
            _mediator = mediator;
            Store = store;
        }
        
        public IWorkflowInstanceStore Store { get; }
        
        public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            await Store.SaveAsync(workflowInstance, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
        }

        public async Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            await Store.DeleteAsync(workflowInstance, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceDeleted(workflowInstance), cancellationToken);
        }

        public Task<WorkflowInstance?> GetByIdAsync(string id, CancellationToken cancellationToken) => Store.GetByIdAsync(id, cancellationToken);
    }
}