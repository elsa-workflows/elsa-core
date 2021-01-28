using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Triggers;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowQueue : IWorkflowQueue
    {
        private readonly IWorkflowSelector _workflowSelector;
        private readonly ICommandSender _commandSender;

        public WorkflowQueue(IWorkflowSelector workflowSelector, ICommandSender commandSender)
        {
            _workflowSelector = workflowSelector;
            _commandSender = commandSender;
        }

        public async Task EnqueueWorkflowsAsync<TTrigger>(
            Func<TTrigger, bool> predicate,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
            where TTrigger : ITrigger
        {
            var results = await _workflowSelector.SelectWorkflowsAsync(predicate, cancellationToken).ToList();
            await EnqueueWorkflowsAsync(results, input, correlationId, contextId, cancellationToken);
        }

        public async Task EnqueueWorkflowsAsync(IEnumerable<WorkflowSelectorResult> results, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default)
        {
            foreach (var result in results)
            {
                if (result.WorkflowInstanceId != null)
                {
                    await EnqueueWorkflowInstance(result.WorkflowInstanceId, result.ActivityId, input, cancellationToken);
                }
                else
                    await EnqueueWorkflowDefinition(result.WorkflowBlueprint.Id, result.WorkflowBlueprint.TenantId, result.ActivityId, input, correlationId, contextId, cancellationToken);

                if (result.Trigger.IsOneOff)
                    await _workflowSelector.RemoveTriggerAsync(result.Trigger, cancellationToken);
            }
        }

        public async Task EnqueueWorkflowInstance(string workflowInstanceId, string activityId, object? input, CancellationToken cancellationToken = default)
        {
            await _commandSender.SendAsync(new RunWorkflowInstance(workflowInstanceId, activityId, input));
        }

        public async Task EnqueueWorkflowDefinition(string workflowDefinitionId, string? tenantId, string activityId, object? input, string? correlationId, string? contextId, CancellationToken cancellationToken = default)
        {
            await _commandSender.SendAsync(new RunWorkflowDefinition(workflowDefinitionId, tenantId, activityId, input, correlationId, contextId));
        }
    }
}