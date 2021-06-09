using System.Threading.Tasks;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class ExecuteWorkflowInstanceRequestConsumer : IHandleMessages<ExecuteWorkflowInstanceRequest>
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;

        public ExecuteWorkflowInstanceRequestConsumer(IWorkflowInstanceExecutor workflowInstanceExecutor) => _workflowInstanceExecutor = workflowInstanceExecutor;

        public async Task Handle(ExecuteWorkflowInstanceRequest message)
        {
            await _workflowInstanceExecutor.ExecuteAsync(message.WorkflowInstanceId, message.ActivityId, message.Input);
        }
    }
}