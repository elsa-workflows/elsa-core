using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultWorkflowDefinitionStorePopulator : IWorkflowDefinitionStorePopulator
{
    private readonly Func<IEnumerable<IWorkflowsProvider>> _workflowDefinitionProviders;
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISystemClock _systemClock;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly ILogger<DefaultWorkflowDefinitionStorePopulator> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowDefinitionStorePopulator"/> class.
    /// </summary>
    public DefaultWorkflowDefinitionStorePopulator(
        Func<IEnumerable<IWorkflowsProvider>> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IActivitySerializer activitySerializer,
        IPayloadSerializer payloadSerializer,
        ISystemClock systemClock,
        IIdentityGraphService identityGraphService,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
        _activitySerializer = activitySerializer;
        _payloadSerializer = payloadSerializer;
        _systemClock = systemClock;
        _identityGraphService = identityGraphService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> PopulateStoreAsync(CancellationToken cancellationToken = default)
    {
        return PopulateStoreAsync(true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> PopulateStoreAsync(bool indexTriggers, CancellationToken cancellationToken = default)
    {
        var providers = _workflowDefinitionProviders();
        var workflowDefinitions = new List<WorkflowDefinition>();
        
        foreach (var provider in providers)
        {
            var results = await provider.GetWorkflowsAsync(cancellationToken).AsTask().ToList();

            foreach (var result in results)
            {
                var workflowDefinition = await AddAsync(result, indexTriggers, cancellationToken);
                workflowDefinitions.Add(workflowDefinition);
            }
        }

        return workflowDefinitions;
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition> AddAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default)
    {
        return AddAsync(materializedWorkflow, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> AddAsync(MaterializedWorkflow materializedWorkflow, bool indexTriggers, CancellationToken cancellationToken = default)
    {
        await AssignIdentities(materializedWorkflow.Workflow, cancellationToken);
        var workflowDefinition = await AddOrUpdateAsync(materializedWorkflow, cancellationToken);

        if (indexTriggers)
            await IndexTriggersAsync(materializedWorkflow, cancellationToken);

        return workflowDefinition;
    }

    private async Task AssignIdentities(Workflow workflow, CancellationToken cancellationToken)
    {
        await _identityGraphService.AssignIdentitiesAsync(workflow, cancellationToken);
    }

    private async Task<WorkflowDefinition> AddOrUpdateAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await AddOrUpdateCoreAsync(materializedWorkflow, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<WorkflowDefinition> AddOrUpdateCoreAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default)
    {
        var workflow = materializedWorkflow.Workflow;
        var definitionId = workflow.Identity.DefinitionId;

        var existingWorkflowLatest = false;
        var existingWorkflowPublished = false;

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

        // Set up a list to collect all workflow definitions to be persisted.
        var workflowDefinitionsToSave = new HashSet<WorkflowDefinition>();

        if (existingDefinitionVersion != null)
        {
            workflowDefinitionsToSave.Add(existingDefinitionVersion);

            if (existingDefinitionVersion.Id != workflow.Identity.Id)
            {
                // It's possible that the imported workflow definition has a different ID than the existing one in the store.
                // In a future update, we might store this discrepancy in a "troubleshooting" table and provide tooling for managing these, and other, discrepancies.
                // See https://github.com/elsa-workflows/elsa-core/issues/5540
                _logger.LogWarning("Workflow with ID {WorkflowId} already exists with a different ID {ExistingWorkflowId}", workflow.Identity.Id, existingDefinitionVersion.Id);
            }
        }

        await UpdateIsLatest();
        await UpdateIsPublished();

        var workflowDefinition = existingDefinitionVersion ?? new WorkflowDefinition
        {
            DefinitionId = workflow.Identity.DefinitionId,
            Id = workflow.Identity.Id,
            Version = workflow.Identity.Version,
            TenantId = workflow.Identity.TenantId,
        };

        workflowDefinition.Description = workflow.WorkflowMetadata.Description;
        workflowDefinition.Name = workflow.WorkflowMetadata.Name;
        workflowDefinition.ToolVersion = workflow.WorkflowMetadata.ToolVersion;
        workflowDefinition.IsLatest = !existingWorkflowLatest;
        workflowDefinition.IsPublished = !existingWorkflowPublished && workflow.Publication.IsPublished;
        workflowDefinition.IsReadonly = workflow.IsReadonly;
        workflowDefinition.IsSystem = workflow.IsSystem;
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

        if (existingDefinitionVersion is null && workflowDefinitionsToSave.Any(w => w.Id == workflowDefinition.Id))
        {
            _logger.LogInformation("Workflow with ID {WorkflowId} already exists", workflowDefinition.Id);
            return workflowDefinition;
        }

        workflowDefinitionsToSave.Add(workflowDefinition);

        var duplicates = workflowDefinitionsToSave.GroupBy(wd => wd.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            throw new Exception($"Unable to update WorkflowDefinition with ids {string.Join(',', duplicates)} multiple times.");
        }

        await _workflowDefinitionStore.SaveManyAsync(workflowDefinitionsToSave, cancellationToken);
        return workflowDefinition;

        async Task UpdateIsLatest()
        {
            // Always try to update the IsLatest property based on the VersionNumber

            // Reset current latest definitions.
            var filter = new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId,
                VersionOptions = VersionOptions.Latest
            };
            var latestWorkflowDefinitions = (await _workflowDefinitionStore.FindManyAsync(filter, cancellationToken)).ToList();

            // If the latest definitions contains definitions with the same ID then we need to replace them with the latest workflow definitions.
            SyncExistingCopies(latestWorkflowDefinitions, workflowDefinitionsToSave);

            foreach (var latestWorkflowDefinition in latestWorkflowDefinitions)
            {
                if (latestWorkflowDefinition.Version > workflow.Identity.Version)
                {
                    _logger.LogWarning("A more recent version of the workflow has been found, overwriting the IsLatest property on the workflow");
                    existingWorkflowLatest = true;
                    continue;
                }

                latestWorkflowDefinition.IsLatest = false;
                workflowDefinitionsToSave.Add(latestWorkflowDefinition);
            }
        }

        async Task UpdateIsPublished()
        {
            // If the workflow being added is configured to be the published version, then we need to reset the current published version.
            if (workflow.Publication.IsPublished)
            {
                // Reset current published definitions.
                var filter = new WorkflowDefinitionFilter
                {
                    DefinitionId = definitionId,
                    VersionOptions = VersionOptions.Published
                };
                var publishedWorkflowDefinitions = (await _workflowDefinitionStore.FindManyAsync(filter, cancellationToken)).ToList();

                // If the published workflow definitions contains definitions with the same ID as definitions in the latest workflow definitions, then we need to replace them with the latest workflow definitions.
                SyncExistingCopies(publishedWorkflowDefinitions, workflowDefinitionsToSave);

                foreach (var publishedWorkflowDefinition in publishedWorkflowDefinitions)
                {
                    if (publishedWorkflowDefinition.Version > workflow.Identity.Version)
                    {
                        _logger.LogWarning("A more recent version of the workflow has been found to be published, overwriting the IsPublished property on the workflow");
                        existingWorkflowPublished = true;
                        continue;
                    }

                    publishedWorkflowDefinition.IsPublished = false;
                    workflowDefinitionsToSave.Add(publishedWorkflowDefinition);
                }
            }
        }
    }

    private async Task IndexTriggersAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(materializedWorkflow.Workflow, cancellationToken);

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