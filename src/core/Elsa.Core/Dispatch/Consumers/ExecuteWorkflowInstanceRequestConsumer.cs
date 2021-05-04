using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class ExecuteWorkflowInstanceRequestConsumer : IHandleMessages<ExecuteWorkflowInstanceRequest>
    {
        private readonly IMediator _mediator;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ICommandSender _commandSender;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public ExecuteWorkflowInstanceRequestConsumer(
            IMediator mediator, 
            IDistributedLockProvider distributedLockProvider,
            ICommandSender commandSender,
            ILogger<ExecuteWorkflowInstanceRequestConsumer> logger)
        {
            _mediator = mediator;
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task Handle(ExecuteWorkflowInstanceRequest message)
        {
            var workflowInstanceId = message.WorkflowInstanceId;
            var lockKey = $"execute-workflow-instance-consumer:{workflowInstanceId}";
            
            _logger.LogDebug("Acquiring lock on workflow instance {WorkflowInstanceId}", workflowInstanceId);
            _stopwatch.Restart();

            await using var handle = await _distributedLockProvider.AcquireLockAsync(lockKey, Duration.FromSeconds(10));
            
            if(handle == null)
            {
                _logger.LogDebug("Failed to acquire lock on workflow instance {WorkflowInstanceId}. Re-queueing message", workflowInstanceId);
                await _commandSender.SendAsync(message);
                return;
            }
            
            await _mediator.Send(message);
            _stopwatch.Stop();
            _logger.LogDebug("Held lock on workflow instance {WorkflowInstanceId} for {ElapsedTime}", workflowInstanceId, _stopwatch.Elapsed);
        }
    }
}