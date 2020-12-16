using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Extensions;
using Elsa.Messages;
using Elsa.Persistence;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class RunWorkflowConsumer : IHandleMessages<RunWorkflow>
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceStore _workflowInstanceManager;

        public RunWorkflowConsumer(IWorkflowRunner workflowRunner, IWorkflowInstanceStore workflowInstanceStore)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceStore;
        }

        public async Task Handle(RunWorkflow message)
        {
            var workflowInstance = await _workflowInstanceManager.FindByIdAsync(message.WorkflowInstanceId);

            if(workflowInstance == null)
                throw new WorkflowException($"No workflow instance with ID {message.WorkflowInstanceId} was found.");
            
            await _workflowRunner.RunWorkflowAsync(
                workflowInstance,
                message.ActivityId,
                message.Input);
        }
    }
}