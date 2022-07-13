using System.Threading.Tasks;
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
            await _workflowInstanceExecutor.ExecuteAsync(message.WorkflowInstanceId, message.ActivityId, message.Input);
        }
    }
}