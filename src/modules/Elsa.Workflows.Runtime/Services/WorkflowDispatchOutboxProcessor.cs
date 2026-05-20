using Elsa.Common.Models;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDispatchOutboxProcessor(
    IWorkflowDispatchOutboxStore store,
    IWorkflowInstanceStore workflowInstanceStore,
    ICommandSender commandSender,
    ILogger<WorkflowDispatchOutboxProcessor> logger) : IWorkflowDispatchOutboxProcessor
{
    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var items = (await store.FindManyAsync(cancellationToken)).OrderBy(x => x.CreatedAt).ToList();

        foreach (var item in items)
        {
            try
            {
                await ProcessAsync(item, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to deliver workflow dispatch outbox item {OutboxItemId}; it will be retried later", item.Id);
            }
        }
    }

    private async Task ProcessAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var owner = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = item.OwnerWorkflowInstanceId }, cancellationToken);

        if (owner?.WorkflowState.HasWorkflowDispatchOutboxItem(item.Id) != true)
        {
            logger.LogDebug("Skipping workflow dispatch outbox item {OutboxItemId} because owner workflow {WorkflowInstanceId} has not committed it", item.Id, item.OwnerWorkflowInstanceId);
            return;
        }

        await SendAsync(item, cancellationToken);
        await store.DeleteAsync(item.Id, cancellationToken);
    }

    private async Task SendAsync(WorkflowDispatchOutboxItem item, CancellationToken cancellationToken)
    {
        var headers = TenantHeaders.CreateHeaders(item.TenantId);

        switch (item.Kind)
        {
            case WorkflowDispatchOutboxItemKind.WorkflowDefinition when item.WorkflowDefinitionCommand != null:
                await commandSender.SendAsync(item.WorkflowDefinitionCommand, CommandStrategy.Background, headers, CancellationToken.None);
                break;
            case WorkflowDispatchOutboxItemKind.WorkflowInstance when item.WorkflowInstanceCommand != null:
                await commandSender.SendAsync(item.WorkflowInstanceCommand, CommandStrategy.Background, headers, CancellationToken.None);
                break;
            case WorkflowDispatchOutboxItemKind.TriggerWorkflows when item.TriggerWorkflowsCommand != null:
                await commandSender.SendAsync(item.TriggerWorkflowsCommand, CommandStrategy.Background, headers, CancellationToken.None);
                break;
            case WorkflowDispatchOutboxItemKind.ResumeWorkflows when item.ResumeWorkflowsCommand != null:
                await commandSender.SendAsync(item.ResumeWorkflowsCommand, CommandStrategy.Background, headers, CancellationToken.None);
                break;
            default:
                throw new InvalidOperationException($"Outbox item {item.Id} does not contain a dispatch command for kind {item.Kind}.");
        }

        logger.LogDebug("Delivered workflow dispatch outbox item {OutboxItemId} for owner workflow {WorkflowInstanceId}", item.Id, item.OwnerWorkflowInstanceId);
    }
}
