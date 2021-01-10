using System;
using System.Diagnostics;
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
        private readonly ICommandSender _commandSender;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public RunWorkflowInstanceConsumer(
            IWorkflowRunner workflowRunner, 
            IWorkflowInstanceStore workflowInstanceStore, 
            IDistributedLockProvider distributedLockProvider,
            ICommandSender commandSender,
            ILogger<RunWorkflowInstanceConsumer> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task Handle(RunWorkflowInstance message)
        {
            var workflowInstanceId = message.WorkflowInstanceId;
            var lockKey = workflowInstanceId;
            
            _logger.LogDebug("Acquiring lock on workflow instance {WorkflowInstanceId}.", workflowInstanceId);
            _stopwatch.Restart();

            if (!await _distributedLockProvider.AcquireLockAsync(lockKey))
            {
                // Reschedule message.
                _logger.LogDebug("Failed to acquire lock on workflow instance {WorkflowInstanceId}. Rescheduling message.", workflowInstanceId);
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _commandSender.SendAsync(message);
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
                await _distributedLockProvider.ReleaseLockAsync(lockKey);
                _stopwatch.Stop();
                _logger.LogDebug("Held lock on workflow instance {WorkflowInstanceId} for {ElapsedTime}.", workflowInstanceId, _stopwatch.Elapsed);
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