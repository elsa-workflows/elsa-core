using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Persistence
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
            var workflowInstance = workflow.Serialize();
            await workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        }
    }
}