using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class ExecuteWorkflowInstanceRequestConsumer : IHandleMessages<ExecuteWorkflowInstanceRequest>
    {
        private readonly IWorkflowInstanceExecutor _workflowInstanceExecutor;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ICommandSender _commandSender;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public ExecuteWorkflowInstanceRequestConsumer(
            IWorkflowInstanceExecutor workflowInstanceExecutor,
            IDistributedLockProvider distributedLockProvider,
            ICommandSender commandSender,
            ILogger<ExecuteWorkflowInstanceRequestConsumer> logger)
        {
            _workflowInstanceExecutor = workflowInstanceExecutor;
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task Handle(ExecuteWorkflowInstanceRequest message)
        {
            var workflowInstanceId = message.WorkflowInstanceId;
            var lockKey = $"execute-workflow-instance-consumer:{workflowInstanceId}";
            
            _logger.LogDebug("Acquiring lock on {LockKey}", lockKey);
            _stopwatch.Restart();

            await using var handle = await _distributedLockProvider.AcquireLockAsync(lockKey, Duration.FromSeconds(10));
            
            if(handle == null)
            {
                _logger.LogDebug("Failed to acquire lock on {LockKey}. Re-queueing message", lockKey);
                await _commandSender.SendAsync(message);
                return;
            }
            
            _logger.LogDebug("Acquired lock on {LockKey}", lockKey);
            await _workflowInstanceExecutor.ExecuteAsync(message.WorkflowInstanceId, message.ActivityId, message.Input);
            _stopwatch.Stop();
            _logger.LogDebug("Held lock on {LockKey} for {ElapsedTime}", lockKey, _stopwatch.Elapsed);
        }
    }
}