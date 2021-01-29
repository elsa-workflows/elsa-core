using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
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
            
            _logger.LogDebug("Acquiring lock on workflow instance {WorkflowInstanceId}", workflowInstanceId);
            _stopwatch.Restart();

            if (!await _distributedLockProvider.AcquireLockAsync(lockKey))
            {
                // Reschedule message.
                _logger.LogDebug("Failed to acquire lock on workflow instance {WorkflowInstanceId}. Rescheduling message", workflowInstanceId);
                await _commandSender.DeferAsync(message, Duration.FromSeconds(5));
                return;
            }
            
            try
            {
                var workflowInstance = await _workflowInstanceManager.FindByIdAsync(message.WorkflowInstanceId);

                if (!ValidatePreconditions(workflowInstanceId, workflowInstance, message.ActivityId))
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
                _logger.LogDebug("Held lock on workflow instance {WorkflowInstanceId} for {ElapsedTime}", workflowInstanceId, _stopwatch.Elapsed);
            }
        }

        private bool ValidatePreconditions(string? workflowInstanceId, WorkflowInstance? workflowInstance, string? activityId)
        {
            if (workflowInstance == null)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist", workflowInstanceId);
                return false;
            }

            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended && workflowInstance.WorkflowStatus != WorkflowStatus.Running)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it has a status other than Suspended or Running. Its actual status is {WorkflowStatus}", workflowInstanceId, workflowInstance.WorkflowStatus);
                return false;
            }

            if (activityId != null)
            {
                var activityIsBlocking = workflowInstance.BlockingActivities.Any(x => x.ActivityId == activityId);
                var activityIsScheduled = workflowInstance.ScheduledActivities.Any(x => x.ActivityId == activityId) || workflowInstance.CurrentActivity?.ActivityId == activityId;
                
                if (!activityIsBlocking && !activityIsScheduled)
                {
                    _logger.LogWarning("Did not run workflow {WorkflowInstanceId} for activity {ActivityId} because the workflow is not blocked on that activity nor is that activity scheduled for execution", workflowInstanceId, activityId);
                    return false;
                }
            }

            return true;
        }
    }
}