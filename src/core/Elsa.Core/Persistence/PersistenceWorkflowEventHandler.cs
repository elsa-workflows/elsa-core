using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Persistence
{
    public class PersistenceWorkflowEventHandler : WorkflowEventHandlerBase
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public PersistenceWorkflowEventHandler(IWorkflowInstanceStore workflowInstanceStore)
        {
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public override async Task InvokingHaltedActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            await SaveWorkflowAsync(workflowExecutionContext.Workflow, cancellationToken);
        }

        public override async Task WorkflowInvokedAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            await SaveWorkflowAsync(workflowExecutionContext.Workflow, cancellationToken);
        }

        private async Task SaveWorkflowAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var workflowInstance = workflow.ToInstance();
            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }
    }
}