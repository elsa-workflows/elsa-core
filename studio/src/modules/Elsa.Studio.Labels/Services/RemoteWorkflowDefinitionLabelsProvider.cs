using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.Labels.Models;

namespace Elsa.Studio.WorkflowContexts.Services;

/// <summary>
/// Provides workflow contexts from a remote API.
/// </summary>
public class RemoteWorkflowDefinitionLabelsProvider : IWorkflowDefinitionLabelsProvider
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowDefinitionLabelsProvider"/> class.
    /// </summary>
    public RemoteWorkflowDefinitionLabelsProvider(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }

    /// <summary>
    /// Updates the labels associated with a workflow definition.
    /// </summary>
    /// <param name="workflowDefinitionId">The ID of the workflow definition to update.</param>
    /// <param name="selectedLabelsIds">The collection of label IDs to associate with the workflow definition.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of updated workflow definition label descriptors.</returns>
    public async Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> UpdateAsync(string workflowDefinitionId, IEnumerable<string> selectedLabelsIds, CancellationToken cancellationToken = default)
    {
        var api = await _backendApiClientProvider.GetApiAsync<IWorkflowDefinitionLabelsApi>(cancellationToken);
        var response = await api.UpdateAsync(workflowDefinitionId, new()
        {
            Id = workflowDefinitionId,
            LabelIds = selectedLabelsIds.ToList()
        }, cancellationToken);

        return await ListAsync(workflowDefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> ListAsync( string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var api = await _backendApiClientProvider.GetApiAsync<IWorkflowDefinitionLabelsApi>(cancellationToken);
        var labelsApi = await _backendApiClientProvider.GetApiAsync<ILabelsApi>(cancellationToken);
        var response = await api.ListAsync( workflowDefinitionId, cancellationToken);
        List<WorkflowDefinitionLabelDescriptor> returnValue = new List<WorkflowDefinitionLabelDescriptor>();
        foreach (var it in response.Items)
        {
            var label = await labelsApi.GetAsync(it, cancellationToken);
            returnValue.Add(new WorkflowDefinitionLabelDescriptor()
            {
                Id = it,
                Name = label.Name ?? label.NormalizedName,
                Color = label.Color,
            });
        }
        return returnValue;
    }
}