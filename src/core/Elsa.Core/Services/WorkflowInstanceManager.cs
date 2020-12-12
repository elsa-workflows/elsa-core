using System.Collections.Generic;
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

        public Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId, CancellationToken cancellationToken = default) =>
            Store.GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId, cancellationToken);

        public Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Store.ListAsync(page, pageSize, cancellationToken);

        public Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) => Store.ListByBlockingActivityTypeAsync(activityType, cancellationToken);

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string? tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) =>
            Store.ListByDefinitionAndStatusAsync(workflowDefinitionId, tenantId, workflowStatus, cancellationToken);

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) => Store.ListByStatusAsync(workflowStatus, cancellationToken);

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default) =>
            Store.ListByDefinitionAsync(workflowDefinitionId, tenantId, cancellationToken);

        public Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) => Store.GetByCorrelationIdAsync(correlationId, status, cancellationToken);

        public Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Store.ListByCorrelationIdAsync(correlationId, status, cancellationToken);

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Store.CountAsync(cancellationToken);
    }
}