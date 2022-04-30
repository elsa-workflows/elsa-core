using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Persistence.Services;
using Elsa.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Open.Linq.AsyncExtensions;

namespace Elsa.Runtime.HostedServices;

/// <summary>
/// Synchronously updates the workflow definition store from <see cref="IWorkflowDefinitionProvider"/> implementations and creates triggers.
/// </summary>
public class PopulateWorkflowDefinitionStore : IHostedService
{
    private readonly IEnumerable<IWorkflowDefinitionProvider> _workflowDefinitionProviders;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly ICommandSender _commandSender;

    public PopulateWorkflowDefinitionStore(
        IEnumerable<IWorkflowDefinitionProvider> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IRequestSender requestSender,
        ICommandSender commandSender)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
        _commandSender = commandSender;
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

        await _commandSender.ExecuteAsync(new SaveWorkflowDefinition(existingDefinition), cancellationToken);
    }

    private async Task IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(workflow, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}