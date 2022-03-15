using System;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Elsa.Services.Dispatch.Consumers
{
    public class ExecuteWorkflowInstanceRequestConsumer : MultitenantConsumer, IHandleMessages<ExecuteWorkflowInstanceRequest>
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;

        public ExecuteWorkflowInstanceRequestConsumer(IWorkflowInstanceExecutor workflowInstanceExecutor, IMessageContext messageContext, IServiceProvider serviceProvider) : base(messageContext, serviceProvider)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
        }

        public async Task Handle(ExecuteWorkflowInstanceRequest message)
        {
            await _workflowInstanceExecutor.ExecuteAsync(message.WorkflowInstanceId, message.ActivityId, message.Input);
        }
    }
}