using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultWorkflowDefinitionStorePopulator : IWorkflowDefinitionStorePopulator
{
    private readonly Func<IEnumerable<IWorkflowProvider>> _workflowDefinitionProviders;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISystemClock _systemClock;
    private readonly IIdentityGraphService _identityGraphService;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowDefinitionStorePopulator(
        Func<IEnumerable<IWorkflowProvider>> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IActivitySerializer activitySerializer,
        IPayloadSerializer payloadSerializer,
        ISystemClock systemClock,
        IIdentityGraphService identityGraphService)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
        _activitySerializer = activitySerializer;
        _payloadSerializer = payloadSerializer;
        _systemClock = systemClock;
        _identityGraphService = identityGraphService;
    }

    /// <inheritdoc />
    public async Task PopulateStoreAsync(CancellationToken cancellationToken = default)
    {
        var providers = _workflowDefinitionProviders();
        foreach (var provider in providers)
        {
            var results = await provider.GetWorkflowsAsync(cancellationToken).AsTask().ToList();

            foreach (var result in results)
            {
                await AssignIdentities(result.Workflow, cancellationToken);
                await AddOrUpdateAsync(result, cancellationToken);
                await IndexTriggersAsync(result, cancellationToken);
            }
        }
    }

    private async Task AssignIdentities(Workflow workflow, CancellationToken cancellationToken)
    {
        await _identityGraphService.AssignIdentitiesAsync(workflow, cancellationToken);
    }

    private async Task AddOrUpdateAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default)
    {
        var workflow = materializedWorkflow.Workflow;
        var definitionId = workflow.Identity.DefinitionId;

        // Serialize materializer context.
        var materializerContext = materializedWorkflow.MaterializerContext;
        var materializerContextJson = materializerContext != null ? _payloadSerializer.Serialize(materializerContext) : default;

        // Serialize the workflow root.
        var workflowJson = _activitySerializer.Serialize(workflow.Root);

        // Check if there's already a workflow definition stored with this definition ID and version.
        var specificVersionFilter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.SpecificVersion(workflow.Identity.Version)
        };
        
        var existingDefinitionVersion = await _workflowDefinitionStore.FindAsync(specificVersionFilter, cancellationToken);

        // Setup a list to collect all workflow definitions to be persisted.
        var workflowDefinitionsToSave = new HashSet<WorkflowDefinition>();
        
        if(existingDefinitionVersion != null)
            workflowDefinitionsToSave.Add(existingDefinitionVersion);
        
        // If the workflow being added is configured to be the latest version, then we need to reset the current latest version.
        if (workflow.Publication.IsLatest)
        {
            // Reset current latest definitions.
            var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };
            var latestWorkflowDefinitions = (await _workflowDefinitionStore.FindManyAsync(filter, cancellationToken)).ToList();

            // If the latest definitions contains definitions with the same ID then we need to replace them with the latest workflow definitions.
            SyncExistingCopies(latestWorkflowDefinitions, workflowDefinitionsToSave);
            
            foreach (var latestWorkflowDefinition in latestWorkflowDefinitions)
            {
                latestWorkflowDefinition.IsLatest = false;
                workflowDefinitionsToSave.Add(latestWorkflowDefinition);
            }
        }

        // If the workflow being added is configured to be the published version, then we need to reset the current published version.
        if (workflow.Publication.IsPublished)
        {
            // Reset current published definitions.
            var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Published };
            var publishedWorkflowDefinitions = (await _workflowDefinitionStore.FindManyAsync(filter, cancellationToken)).ToList();

            // If the published workflow definitions contains definitions with the same ID as definitions in the latest workflow definitions, then we need to replace them with the latest workflow definitions.
            SyncExistingCopies(publishedWorkflowDefinitions, workflowDefinitionsToSave);
            
            foreach (var publishedWorkflowDefinition in publishedWorkflowDefinitions)
            {
                publishedWorkflowDefinition.IsPublished = false;
                workflowDefinitionsToSave.Add(publishedWorkflowDefinition);
            }
        }

        var workflowDefinition = existingDefinitionVersion ?? new WorkflowDefinition
        {
            DefinitionId = workflow.Identity.DefinitionId,
            Id = workflow.Identity.Id,
            Version = workflow.Identity.Version
        };
        
        workflowDefinition.Description = workflow.WorkflowMetadata.Description;
        workflowDefinition.Name = workflow.WorkflowMetadata.Name;
        workflowDefinition.ToolVersion = workflow.WorkflowMetadata.ToolVersion;
        workflowDefinition.IsLatest = workflow.Publication.IsLatest;
        workflowDefinition.IsPublished = workflow.Publication.IsPublished;
        workflowDefinition.IsReadonly = workflow.IsReadonly;
        workflowDefinition.CustomProperties = workflow.CustomProperties;
        workflowDefinition.Variables = workflow.Variables;
        workflowDefinition.Inputs = workflow.Inputs;
        workflowDefinition.Outputs = workflow.Outputs;
        workflowDefinition.Outcomes = workflow.Outcomes;
        workflowDefinition.StringData = workflowJson;
        workflowDefinition.CreatedAt = workflow.WorkflowMetadata.CreatedAt == default ? _systemClock.UtcNow : workflow.WorkflowMetadata.CreatedAt;
        workflowDefinition.Options = workflow.Options;
        workflowDefinition.ProviderName = materializedWorkflow.ProviderName;
        workflowDefinition.MaterializerContext = materializerContextJson;
        workflowDefinition.MaterializerName = materializedWorkflow.MaterializerName;

        workflowDefinitionsToSave.Add(workflowDefinition);
        await _workflowDefinitionStore.SaveManyAsync(workflowDefinitionsToSave, cancellationToken);
    }

    private async Task IndexTriggersAsync(MaterializedWorkflow workflow, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(workflow.Workflow, cancellationToken);

    /// <summary>
    /// Syncs the items in the primary list with existing items in the secondary list, even when the object instances are not the same (but their IDs are).
    /// </summary>
    private void SyncExistingCopies(List<WorkflowDefinition> primary, HashSet<WorkflowDefinition> secondary)
    {
        var ids = secondary.Select(x => x.Id).Distinct().ToList();
        var latestWorkflowDefinitions = primary.Where(x => ids.Contains(x.Id)).ToList();
        primary.RemoveAll(x => latestWorkflowDefinitions.Contains(x));
        primary.AddRange(secondary.Where(x => ids.Contains(x.Id)));   
    }
}