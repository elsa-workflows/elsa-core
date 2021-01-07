using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class RunWorkflowInstanceConsumer : IHandleMessages<RunWorkflowInstance>
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceStore _workflowInstanceManager;
        private readonly ILogger _logger;

        public RunWorkflowInstanceConsumer(IWorkflowRunner workflowRunner, IWorkflowInstanceStore workflowInstanceStore, ILogger<RunWorkflowInstanceConsumer> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceStore;
            _logger = logger;
        }

        public async Task Handle(RunWorkflowInstance message)
        {
            var workflowInstanceId = message.WorkflowInstanceId;
            var workflowInstance = await _workflowInstanceManager.FindByIdAsync(message.WorkflowInstanceId);

            if (!ValidatePreconditions(workflowInstanceId, workflowInstance))
                return;
            
            await _workflowRunner.RunWorkflowAsync(
                workflowInstance!,
                message.ActivityId,
                message.Input);
        }
        
        private bool ValidatePreconditions(string? workflowInstanceId, WorkflowInstance? workflowInstance)
        {
            if (workflowInstance == null)
            {
                _logger.LogError("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist.", workflowInstanceId);
                return false;
            }

            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it has a status other than Suspended. Its actual status is {WorkflowStatus}", workflowInstanceId, workflowInstance.WorkflowStatus);
                return false;
            }

            return true;
        }
    }
}