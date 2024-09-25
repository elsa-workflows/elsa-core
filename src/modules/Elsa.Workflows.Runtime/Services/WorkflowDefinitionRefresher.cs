using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowDefinitionsRefresher(IWorkflowDefinitionStore store, ITriggerIndexer triggerIndexer, INotificationSender notificationSender) : IWorkflowDefinitionsRefresher
{
    /// <inheritdoc />
    public async Task<RefreshWorkflowDefinitionsResponse> RefreshWorkflowDefinitionsAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionIds = request.DefinitionIds,
            VersionOptions = VersionOptions.Published
        };

        var currentPage = 0;
        var processedWorkflowDefinitions = new List<WorkflowDefinition>();
        var batchSize = request.BatchSize;
        var order = new WorkflowDefinitionOrder<string>(x => x.Id, OrderDirection.Ascending);

        while (!cancellationToken.IsCancellationRequested)
        {
            var pageArgs = PageArgs.FromPage(currentPage, batchSize);
            var definitions = await store.FindManyAsync(filter, order, pageArgs, cancellationToken);

            if (definitions.Items.Count == 0)
                break;

            await IndexWorkflowTriggersAsync(definitions.Items, cancellationToken);
            processedWorkflowDefinitions.AddRange(definitions.Items);
            currentPage++;

            if (definitions.Items.Count < batchSize)
                break;
        }

        var processedWorkflowDefinitionIds = processedWorkflowDefinitions.Select(x => x.DefinitionId).ToList();
        var notification = new WorkflowDefinitionsRefreshed(processedWorkflowDefinitionIds);
        await notificationSender.SendAsync(notification, cancellationToken);
        return new RefreshWorkflowDefinitionsResponse(processedWorkflowDefinitionIds, request.DefinitionIds?.Except(processedWorkflowDefinitionIds)?.ToList() ?? []);
    }

    private async Task IndexWorkflowTriggersAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        foreach (var definition in definitions)
            await triggerIndexer.IndexTriggersAsync(definition, cancellationToken);
    }
}