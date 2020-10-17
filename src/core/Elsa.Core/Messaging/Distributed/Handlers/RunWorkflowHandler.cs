using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Messages;
using Elsa.Queries;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Messaging.Distributed.Handlers
{
    public class RunWorkflowHandler : IHandleMessages<RunWorkflow>
    {
        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;

        public RunWorkflowHandler(IWorkflowHost workflowHost, IWorkflowInstanceManager workflowInstanceManager)
        {
            _workflowHost = workflowHost;
            _workflowInstanceManager = workflowInstanceManager;
        }

        public async Task Handle(RunWorkflow message)
        {
            var workflowInstance = await _workflowInstanceManager.GetByWorkflowInstanceIdAsync(message.WorkflowInstanceId);

            if(workflowInstance == null)
                throw new WorkflowException($"No workflow instance with ID {message.WorkflowInstanceId} was found.");
            
            await _workflowHost.RunWorkflowAsync(
                workflowInstance,
                message.ActivityId,
                message.Input);
        }
    }
}