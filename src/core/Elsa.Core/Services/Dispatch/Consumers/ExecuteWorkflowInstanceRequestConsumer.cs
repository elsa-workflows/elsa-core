using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Rebus.Handlers;

namespace Elsa.Services.Dispatch.Consumers
{
    public class ExecuteWorkflowInstanceRequestConsumer : IHandleMessages<ExecuteWorkflowInstanceRequest>
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;

        public ExecuteWorkflowInstanceRequestConsumer(IWorkflowInstanceExecutor workflowInstanceExecutor)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
        }

        public async Task Handle(ExecuteWorkflowInstanceRequest message)
        {
            var result = await _workflowInstanceExecutor.ExecuteAsync(message.WorkflowInstanceId, message.ActivityId, message.Input);

            // If the workflow faulted, we do not want to mark the current message as completed.
            // Instead, make sure it ends up in the error queue for further inspection by e.g. IT staff.
            if (result.WorkflowInstance?.WorkflowStatus == WorkflowStatus.Faulted)
                // WorkflowException implements IFailFastException, which instructs Rebus to not keep retrying.  
                throw new WorkflowException($"Failed to execute workflow instance {message.WorkflowInstanceId}");
        }
    }
}