using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Messages;
using Elsa.Repositories;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class RunWorkflowConsumer : IHandleMessages<RunWorkflow>
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceRepository _workflowInstanceManager;

        public RunWorkflowConsumer(IWorkflowRunner workflowRunner, IWorkflowInstanceRepository workflowInstanceRepository)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceRepository;
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
    }
}