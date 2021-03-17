using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Persistence;
using MediatR;

namespace Elsa.Handlers
{
    public class ExecuteWorkflowsPostSuspensionTasks : INotificationHandler<WorkflowSuspended>
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public ExecuteWorkflowsPostSuspensionTasks(IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            try
            {
                await notification.WorkflowExecutionContext.ProcessRegisteredTasksAsync(cancellationToken);
            }
            catch (Exception e)
            {
                notification.WorkflowExecutionContext.Fault(e, "Error occurred while executing post-suspension task", null, null, false);
                await _workflowInstanceStore.SaveAsync(notification.WorkflowExecutionContext.WorkflowInstance, cancellationToken);
            }
        }
    }
}