using System;
using System.Threading.Tasks;
using Elsa.DistributedLock;
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
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger _logger;

        public RunWorkflowInstanceConsumer(
            IWorkflowRunner workflowRunner, 
            IWorkflowInstanceStore workflowInstanceStore, 
            IDistributedLockProvider distributedLockProvider, 
            IEventPublisher eventPublisher,
            ILogger<RunWorkflowInstanceConsumer> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task Handle(RunWorkflowInstance message)
        {
            var workflowInstanceId = message.WorkflowInstanceId;
            
            _logger.LogDebug("Acquiring lock on workflow instance {WorkflowInstanceId}.", workflowInstanceId);

            if (!await _distributedLockProvider.AcquireLockAsync(workflowInstanceId))
            {
                // Reschedule message.
                _logger.LogDebug("Failed to acquire lock on workflow instance {WorkflowInstanceId}. Rescheduling message.", workflowInstanceId);
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _eventPublisher.PublishAsync(message);
                return;
            }
            
            try
            {
                var workflowInstance = await _workflowInstanceManager.FindByIdAsync(message.WorkflowInstanceId);

                if (!ValidatePreconditions(workflowInstanceId, workflowInstance))
                    return;

                await _workflowRunner.RunWorkflowAsync(
                    workflowInstance!,
                    message.ActivityId,
                    message.Input);
            }
            finally
            {
                await _distributedLockProvider.ReleaseLockAsync(workflowInstanceId);
            }
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