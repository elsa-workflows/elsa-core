using System;
using System.Reflection;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.DistributedLock;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
using Rebus.Handlers;

namespace Elsa.Activities.Telnyx.Webhooks.Consumers
{
    internal class TriggerWorkflows : IHandleMessages<TelnyxWebhookReceived>
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;

        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ICommandSender _commandSender;
        private readonly ILogger<TriggerWorkflows> _logger;

        public TriggerWorkflows(IWorkflowRunner workflowRunner, IWorkflowInstanceStore workflowInstanceStore, IDistributedLockProvider distributedLockProvider, ICommandSender commandSender, ILogger<TriggerWorkflows> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowInstanceStore = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _commandSender = commandSender;
            _logger = logger;
        }

        public async Task Handle(TelnyxWebhookReceived message)
        {
            var webhook = message.Webhook;
            var eventType = webhook.Data.EventType;
            var payload = message.Webhook.Data.Payload;
            var payloadAttribute = payload.GetType().GetCustomAttribute<PayloadAttribute>();

            if (payloadAttribute == null)
            {
                _logger.LogWarning("The received payload type '{PayloadTypeName}' does not have a PayloadAttribute containing metadata, which means it's an unsupported event", payload.GetType().Name);
                return;
            }
            
            var activityType = payloadAttribute.ActivityType;
            var correlationId = (payload as ICorrelationId)?.CorrelationId;
            var lockKey = $"telnyx:trigger-workflows:correlation-{correlationId}";
            
            if (!await _distributedLockProvider.AcquireLockAsync(lockKey))
            {
                _logger.LogDebug("Lock {LockKey} already taken", lockKey);

                await Task.Delay(TimeSpan.FromSeconds(5));
                await _commandSender.SendAsync(message);
                return;
            }

            try
            {
                var correlatedWorkflowInstanceCount = await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId));

                if (correlatedWorkflowInstanceCount > 0)
                    await _workflowRunner.ResumeWorkflowsAsync(activityType, new NotificationBookmark(eventType, correlationId), TenantId, webhook, correlationId);
                else
                    await _workflowRunner.StartWorkflowsAsync(activityType, new NotificationBookmark(eventType), TenantId, webhook);
            }
            finally
            {
                await _distributedLockProvider.ReleaseLockAsync(lockKey);
            }
        }
    }
}