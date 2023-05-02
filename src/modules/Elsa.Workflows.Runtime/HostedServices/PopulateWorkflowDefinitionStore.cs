using Elsa.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Synchronously updates the workflow definition store from <see cref="IWorkflowDefinitionProvider"/> implementations and creates triggers.
/// </summary>
public class PopulateWorkflowDefinitionStore : IHostedService
{
    private readonly Func<IEnumerable<IWorkflowDefinitionProvider>> _workflowDefinitionProviders;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PopulateWorkflowDefinitionStore(
        Func<IEnumerable<IWorkflowDefinitionProvider>> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var providers = _workflowDefinitionProviders();
        foreach (var provider in providers)
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
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definition.DefinitionId, 
            VersionOptions = VersionOptions.SpecificVersion(definition.Version)
        };
        
        var existingDefinition = await _workflowDefinitionStore.FindAsync(filter, cancellationToken);

        if (existingDefinition == null)
        {
            existingDefinition = definition;
        }
        else
        {
            existingDefinition.Description = definition.Description;
            existingDefinition.Name = definition.Name;
            existingDefinition.CustomProperties = definition.CustomProperties;
            existingDefinition.Variables = definition.Variables;
            existingDefinition.BinaryData = definition.BinaryData;
            existingDefinition.StringData = definition.StringData;
            existingDefinition.MaterializerContext = definition.MaterializerContext;
            existingDefinition.MaterializerName = definition.MaterializerName;
        }

        await _workflowDefinitionStore.SaveAsync(existingDefinition, cancellationToken);
    }

    private async Task IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(workflow, cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}