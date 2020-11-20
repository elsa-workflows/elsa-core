using System;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Extensions;
using Elsa.Messages;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class RunWorkflowConsumer : IHandleMessages<RunWorkflow>, IDisposable
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;

        public RunWorkflowConsumer(IWorkflowRunner workflowRunner, IWorkflowInstanceManager workflowInstanceManager)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceManager;
        }

        public async Task Handle(RunWorkflow message)
        {
            var workflowInstance = await _workflowInstanceManager.GetByIdAsync(message.WorkflowInstanceId);

            if(workflowInstance == null)
                throw new WorkflowException($"No workflow instance with ID {message.WorkflowInstanceId} was found.");
            
            await _workflowRunner.RunWorkflowAsync(
                workflowInstance,
                message.ActivityId,
                message.Input);
        }

        public void Dispose()
        {
            
        }
    }
}