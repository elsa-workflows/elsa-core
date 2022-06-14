using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Synchronously updates the workflow definition store from <see cref="IWorkflowDefinitionProvider"/> implementations and creates triggers.
/// </summary>
public class PopulateWorkflowDefinitionStore : IHostedService
{
    private readonly IEnumerable<IWorkflowDefinitionProvider> _workflowDefinitionProviders;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;

    public PopulateWorkflowDefinitionStore(
        IEnumerable<IWorkflowDefinitionProvider> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _workflowDefinitionProviders)
        {
            var results = await provider.GetWorkflowDefinitionsAsync(cancellationToken).AsTask().ToList();

            foreach (var result in results)
            {
                await AddOrUpdateAsync(result.Definition, cancellationToken);
                await IndexTriggersAsync(result.Workflow, cancellationToken);
            }
        }
    }

    private async Task AddOrUpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        // Check if there's already a workflow definition by the definition ID and version.
        var existingDefinition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
            definition.DefinitionId,
            VersionOptions.SpecificVersion(definition.Version),
            cancellationToken);

        if (existingDefinition == null)
        {
            existingDefinition = definition;
        }
        else
        {
            existingDefinition.Description = definition.Description;
            existingDefinition.Name = definition.Name;
            existingDefinition.Metadata = definition.Metadata;
            existingDefinition.Variables = definition.Variables;
            existingDefinition.ApplicationProperties = definition.ApplicationProperties;
            existingDefinition.BinaryData = definition.BinaryData;
            existingDefinition.StringData = definition.StringData;
            existingDefinition.MaterializerContext = definition.MaterializerContext;
            existingDefinition.MaterializerName = definition.MaterializerName;
        }

        await _workflowDefinitionStore.SaveAsync(existingDefinition, cancellationToken);
    }

    private async Task IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(workflow, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}