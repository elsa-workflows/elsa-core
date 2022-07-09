using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using MediatR;
using NodaTime;

namespace Elsa.Services.Workflows
{
    public class WorkflowInstanceCanceller : IWorkflowInstanceCanceller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IClock _clock;
        private readonly IMediator _mediator;

        public WorkflowInstanceCanceller(IWorkflowInstanceStore workflowInstanceStore, IClock clock, IMediator mediator)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _clock = clock;
            _mediator = mediator;
        }
        
        public async Task<CancelWorkflowInstanceResult> CancelAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return new CancelWorkflowInstanceResult(CancelWorkflowInstanceResultStatus.NotFound, null);

            if (workflowInstance.WorkflowStatus != WorkflowStatus.Idle && workflowInstance.WorkflowStatus != WorkflowStatus.Running && workflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                return new CancelWorkflowInstanceResult(CancelWorkflowInstanceResultStatus.InvalidStatus, workflowInstance);

            workflowInstance.BlockingActivities = new HashSet<BlockingActivity>();
            workflowInstance.CurrentActivity = null;
            workflowInstance.WorkflowStatus = WorkflowStatus.Cancelled;
            workflowInstance.CancelledAt = _clock.GetCurrentInstant();

            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceCancelled(workflowInstance), cancellationToken);
            
            return new CancelWorkflowInstanceResult(CancelWorkflowInstanceResultStatus.Ok, workflowInstance);
        }
    }
}