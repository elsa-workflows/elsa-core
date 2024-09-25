using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever workflow definitions are reloaded.
[PublicAPI]
public class RefreshActivityRegistry(IWorkflowDefinitionActivityRegistryUpdater workflowDefinitionActivityRegistryUpdater) : INotificationHandler<WorkflowDefinitionsReloaded>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsReloaded notification, CancellationToken cancellationToken)
    {
        foreach (var reloadedWorkflowDefinition in notification.ReloadedWorkflowDefinitions)
            await UpdateDefinition(reloadedWorkflowDefinition.DefinitionVersionId, reloadedWorkflowDefinition.UsableAsActivity);
    }

    private Task UpdateDefinition(string definitionVersionId, bool? usableAsActivity)
    {
        // A workflow should remain in the activity registry unless no longer being marked as an activity.
        if (usableAsActivity.GetValueOrDefault())
            return workflowDefinitionActivityRegistryUpdater.AddToRegistry(definitionVersionId);

        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(definitionVersionId);
        return Task.CompletedTask;
    }
}