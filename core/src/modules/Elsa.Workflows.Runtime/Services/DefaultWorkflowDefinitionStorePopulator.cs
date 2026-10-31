using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Providers;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IClrWorkflowDefinitionTenantRestamper? _clrTenantRestamper;
    private readonly TenantAgnosticWorkflowsCache? _tenantAgnosticWorkflowsCache;
    private readonly ILogger<DefaultWorkflowDefinitionStorePopulator> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowDefinitionStorePopulator"/> class that builds the workflows
    /// of <see cref="ITenantAgnosticWorkflowsProvider"/>s once through <paramref name="tenantAgnosticWorkflowsCache"/>.
    /// </summary>
    internal DefaultWorkflowDefinitionStorePopulator(
        Func<IEnumerable<IWorkflowsProvider>> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IActivitySerializer activitySerializer,
        IPayloadSerializer payloadSerializer,
        ISystemClock systemClock,
        IIdentityGraphService identityGraphService,
        ITenantAccessor tenantAccessor,
        TenantAgnosticWorkflowsCache tenantAgnosticWorkflowsCache,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger,
        IClrWorkflowDefinitionTenantRestamper? clrTenantRestamper)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
        _activitySerializer = activitySerializer;
        _payloadSerializer = payloadSerializer;
        _systemClock = systemClock;
        _identityGraphService = identityGraphService;
        _tenantAccessor = tenantAccessor;
        _tenantAgnosticWorkflowsCache = tenantAgnosticWorkflowsCache;
        _logger = logger;
        _clrTenantRestamper = clrTenantRestamper;
    }

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
        ITenantAccessor tenantAccessor,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger)
        : this(
            workflowDefinitionProviders,
            triggerIndexer,
            workflowDefinitionStore,
            activitySerializer,
            payloadSerializer,
            systemClock,
            identityGraphService,
            tenantAccessor,
            logger,
            clrTenantRestamper: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowDefinitionStorePopulator"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DefaultWorkflowDefinitionStorePopulator(
        Func<IEnumerable<IWorkflowsProvider>> workflowDefinitionProviders,
        ITriggerIndexer triggerIndexer,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IActivitySerializer activitySerializer,
        IPayloadSerializer payloadSerializer,
        ISystemClock systemClock,
        IIdentityGraphService identityGraphService,
        ITenantAccessor tenantAccessor,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger,
        IClrWorkflowDefinitionTenantRestamper? clrTenantRestamper = null)
    {
        _workflowDefinitionProviders = workflowDefinitionProviders;
        _triggerIndexer = triggerIndexer;
        _workflowDefinitionStore = workflowDefinitionStore;
        _activitySerializer = activitySerializer;
        _payloadSerializer = payloadSerializer;
        _systemClock = systemClock;
        _identityGraphService = identityGraphService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
        _clrTenantRestamper = clrTenantRestamper;
    }

    /// <summary>
    /// Creates a populator that shares the container's <see cref="TenantAgnosticWorkflowsCache"/>, so that tenant-agnostic
    /// workflows are built once for all tenants.
    /// </summary>
    internal static DefaultWorkflowDefinitionStorePopulator Create(IServiceProvider serviceProvider)
    {
        return new(
            serviceProvider.GetRequiredService<Func<IEnumerable<IWorkflowsProvider>>>(),
            serviceProvider.GetRequiredService<ITriggerIndexer>(),
            serviceProvider.GetRequiredService<IWorkflowDefinitionStore>(),
            serviceProvider.GetRequiredService<IActivitySerializer>(),
            serviceProvider.GetRequiredService<IPayloadSerializer>(),
            serviceProvider.GetRequiredService<ISystemClock>(),
            serviceProvider.GetRequiredService<IIdentityGraphService>(),
            serviceProvider.GetRequiredService<ITenantAccessor>(),
            serviceProvider.GetRequiredService<TenantAgnosticWorkflowsCache>(),
            serviceProvider.GetRequiredService<ILogger<DefaultWorkflowDefinitionStorePopulator>>(),
            serviceProvider.GetService<IClrWorkflowDefinitionTenantRestamper>());
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
        var currentTenantId = _tenantAccessor.TenantId;

        foreach (var provider in providers)
        {
            IReadOnlyCollection<MaterializedWorkflow> results;

            if (_tenantAgnosticWorkflowsCache != null && provider is ITenantAgnosticWorkflowsProvider)
            {
                // Only an indexing pass is cached: the non-indexing pass runs before tenant-specific activity descriptors are registered, so its build may be incomplete.
                var importedWorkflows = await _tenantAgnosticWorkflowsCache.GetOrBuildAsync(provider.GetType(), () => BuildImportedWorkflowsAsync(provider, cancellationToken), indexTriggers, cancellationToken);

                if (importedWorkflows.All(x => x.IsTenantAgnostic))
                {
                    await ImportTenantAgnosticWorkflowsAsync(importedWorkflows, indexTriggers, workflowDefinitions, cancellationToken);
                    continue;
                }

                results = importedWorkflows.Select(x => x.MaterializedWorkflow).ToList();
            }
            else
            {
                results = await provider.GetWorkflowsAsync(cancellationToken).AsTask().ToList();
            }

            foreach (var result in results)
            {
                await RestampClrTenantAsync(result, cancellationToken);

                // Normalize tenant IDs for comparison (null becomes empty string)
                var definitionTenantId = result.Workflow.Identity.TenantId.NormalizeTenantId();

                // Only import workflows belonging to the current tenant or tenant-agnostic workflows (TenantId = "*").
                if (definitionTenantId != currentTenantId && definitionTenantId != Tenant.AgnosticTenantId)
                {
                    _logger.LogDebug(
                        "Skipping adding workflow {WorkflowId} from provider {Provider} because it belongs to tenant '{WorkflowTenantId}' but current tenant is '{CurrentTenantId}'",
                        result.Workflow.Identity.DefinitionId,
                        provider.Name,
                        result.Workflow.Identity.TenantId,
                        currentTenantId);
                    continue;
                }

                var addResult = await AddAsync(result, indexTriggers, cancellationToken);
                addResult.OnSuccess(workflowDefinition => workflowDefinitions.Add(workflowDefinition));
            }
        }

        return workflowDefinitions;
    }

    /// <summary>
    /// Builds the workflows of the provider. When all of them are tenant-agnostic, they are also serialized ahead of time so
    /// that they can be compared with the stored definitions; a workflow that cannot be serialized gets no content and is
    /// always imported.
    /// </summary>
    private async Task<IReadOnlyList<ImportedWorkflow>> BuildImportedWorkflowsAsync(IWorkflowsProvider provider, CancellationToken cancellationToken)
    {
        var results = await provider.GetWorkflowsAsync(cancellationToken).AsTask().ToList();
        var allTenantAgnostic = results.All(IsTenantAgnostic);
        var importedWorkflows = new List<ImportedWorkflow>();
        var persistedStates = allTenantAgnostic ? GetPersistedStates(results) : [];

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            if (!allTenantAgnostic)
            {
                importedWorkflows.Add(new(result, IsTenantAgnostic(result), false, false, null, null, null));
                continue;
            }

            // The root activity is serialized with its identities assigned, like it is when the definition is saved.
            await AssignIdentities(result.Workflow, cancellationToken);

            try
            {
                importedWorkflows.Add(new(result, true, persistedStates[i].IsLatest, persistedStates[i].IsPublished, GetStringData(result), GetMaterializerContext(result), SerializeContent(result.Workflow)));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Unable to serialize workflow {WorkflowId} of provider {Provider} ahead of time; it is imported without comparing it with the stored definition", result.Workflow.Identity.DefinitionId, provider.Name);
                importedWorkflows.Add(new(result, true, persistedStates[i].IsLatest, persistedStates[i].IsPublished, null, null, null));
            }
        }

        return importedWorkflows;
    }

    /// <summary>
    /// Predicts the IsLatest and IsPublished values that AddOrUpdateCoreAsync persists for each result, replaying the results
    /// in order: a version is latest unless a newer one exists, and a published version unpublishes the older published ones
    /// unless a newer published one exists. Keep in sync with UpdateIsLatest and UpdateIsPublished in AddOrUpdateCoreAsync.
    /// </summary>
    private static List<(bool IsLatest, bool IsPublished)> GetPersistedStates(IReadOnlyList<MaterializedWorkflow> results)
    {
        var states = new List<(bool IsLatest, bool IsPublished)>();

        for (var i = 0; i < results.Count; i++)
        {
            var identity = results[i].Workflow.Identity;
            var isPublished = results[i].Workflow.Publication.IsPublished;
            var siblings = Enumerable.Range(0, i).Where(x => results[x].Workflow.Identity.DefinitionId == identity.DefinitionId).ToList();
            var hasNewerLatest = siblings.Any(x => states[x].IsLatest && results[x].Workflow.Identity.Version > identity.Version);
            var hasNewerPublished = siblings.Any(x => states[x].IsPublished && results[x].Workflow.Identity.Version > identity.Version);

            foreach (var sibling in siblings.Where(x => results[x].Workflow.Identity.Version <= identity.Version))
            {
                states[sibling] = (false, states[sibling].IsPublished && !isPublished);
            }

            states.Add((!hasNewerLatest, isPublished && !hasNewerPublished));
        }

        return states;
    }

    /// <summary>
    /// Reuses each stored definition that already matches its imported workflow and saves the others. Triggers are indexed
    /// for the current tenant in both cases.
    /// </summary>
    private async Task ImportTenantAgnosticWorkflowsAsync(IReadOnlyList<ImportedWorkflow> importedWorkflows, bool indexTriggers, List<WorkflowDefinition> workflowDefinitions, CancellationToken cancellationToken)
    {
        if (importedWorkflows.Count == 0)
        {
            return;
        }

        var storedDefinitions = await FindStoredDefinitionsAsync(importedWorkflows, cancellationToken);

        foreach (var importedWorkflow in importedWorkflows)
        {
            if (storedDefinitions.TryGetValue(importedWorkflow.MaterializedWorkflow.Workflow.Identity.Id, out var storedDefinition) && IsCurrent(storedDefinition, importedWorkflow))
            {
                workflowDefinitions.Add(storedDefinition);

                if (indexTriggers && storedDefinition.IsPublished)
                {
                    await _triggerIndexer.IndexTriggersAsync(storedDefinition, cancellationToken);
                }

                continue;
            }

            var addResult = await AddAsync(importedWorkflow.MaterializedWorkflow, indexTriggers, cancellationToken);
            addResult.OnSuccess(workflowDefinition => workflowDefinitions.Add(workflowDefinition));
        }
    }

    /// <summary>
    /// Returns the stored definitions of the imported workflows by ID. Returns an empty dictionary when the lookup fails, so
    /// that every workflow is imported.
    /// </summary>
    private async Task<Dictionary<string, WorkflowDefinition>> FindStoredDefinitionsAsync(IReadOnlyList<ImportedWorkflow> importedWorkflows, CancellationToken cancellationToken)
    {
        try
        {
            var filter = new WorkflowDefinitionFilter { Ids = importedWorkflows.Select(x => x.MaterializedWorkflow.Workflow.Identity.Id).ToList() };
            return (await _workflowDefinitionStore.FindManyAsync(filter, cancellationToken)).ToDictionary(x => x.Id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Unable to look up the stored workflow definitions of a tenant-agnostic provider; importing all of its workflows");
            return [];
        }
    }

    /// <summary>
    /// Returns whether the workflow belongs to every tenant (<see cref="Tenant.AgnosticTenantId"/>).
    /// </summary>
    private static bool IsTenantAgnostic(MaterializedWorkflow materializedWorkflow) => materializedWorkflow.Workflow.Identity.TenantId.NormalizeTenantId() == Tenant.AgnosticTenantId;

    /// <summary>
    /// Returns whether the stored definition already holds every value AddOrUpdateCoreAsync would persist for the imported
    /// workflow, except for timestamps.
    /// </summary>
    private bool IsCurrent(WorkflowDefinition stored, ImportedWorkflow imported)
    {
        var materializedWorkflow = imported.MaterializedWorkflow;
        var workflow = materializedWorkflow.Workflow;

        return imported.Content != null
               && stored.DefinitionId == workflow.Identity.DefinitionId
               && stored.Version == workflow.Identity.Version
               && stored.TenantId == workflow.Identity.TenantId
               && stored.Name == workflow.WorkflowMetadata.Name
               && stored.Description == workflow.WorkflowMetadata.Description
               && stored.ToolVersion == workflow.WorkflowMetadata.ToolVersion
               && stored.IsLatest == imported.IsLatest
               && stored.IsPublished == imported.IsPublished
               && stored.IsReadonly == workflow.IsReadonly
               && stored.IsSystem == workflow.IsSystem
               && stored.StringData == imported.StringData
               && stored.OriginalSource == materializedWorkflow.OriginalSource
               && stored.ProviderName == materializedWorkflow.ProviderName
               && stored.MaterializerName == materializedWorkflow.MaterializerName
               && stored.MaterializerContext == imported.MaterializerContext
               && HasContent(stored, imported.Content);
    }

    /// <summary>
    /// Returns whether the stored definition serializes to the specified content. A stored definition that cannot be
    /// serialized is treated as different, so that it is saved again.
    /// </summary>
    private bool HasContent(WorkflowDefinition stored, string content)
    {
        try
        {
            return SerializeContent(stored.Options, stored.Variables, stored.Inputs, stored.Outputs, stored.Outcomes, stored.CustomProperties) == content;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Unable to serialize stored workflow definition {WorkflowDefinitionId} for comparison; it is imported again", stored.Id);
            return false;
        }
    }

    /// <summary>
    /// Serializes the values of the workflow that a definition stores outside of its root activity, for comparison only.
    /// </summary>
    private string SerializeContent(Workflow workflow) => SerializeContent(workflow.Options, workflow.Variables, workflow.Inputs, workflow.Outputs, workflow.Outcomes, workflow.CustomProperties);

    /// <inheritdoc cref="SerializeContent(Workflow)"/>
    private string SerializeContent(WorkflowOptions options, ICollection<Variable> variables, ICollection<InputDefinition> inputs, ICollection<OutputDefinition> outputs, ICollection<string> outcomes, IDictionary<string, object> customProperties) =>
        _payloadSerializer.Serialize(new WorkflowContent(options, variables, inputs, outputs, outcomes, customProperties));

    private string? GetMaterializerContext(MaterializedWorkflow materializedWorkflow)
    {
        var materializerContext = materializedWorkflow.MaterializerContext;
        return materializerContext != null ? _payloadSerializer.Serialize(materializerContext) : null;
    }

    private string? GetStringData(MaterializedWorkflow materializedWorkflow)
    {
        // For JSON workflows, StringData holds the serialized root.
        // For other formats (ElsaScript, YAML, etc.), only OriginalSource is needed.
        if (materializedWorkflow.OriginalSource != null)
        {
            return materializedWorkflow.MaterializerName == "Json" ? _activitySerializer.Serialize(materializedWorkflow.Workflow.Root) : null;
        }

        return _activitySerializer.Serialize(materializedWorkflow.Workflow.Root);
    }

    private record WorkflowContent(WorkflowOptions Options, ICollection<Variable> Variables, ICollection<InputDefinition> Inputs, ICollection<OutputDefinition> Outputs, ICollection<string> Outcomes, IDictionary<string, object> CustomProperties);

    /// <inheritdoc />
    public Task<Result<WorkflowDefinition>> AddAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default)
    {
        return AddAsync(materializedWorkflow, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition>> AddAsync(MaterializedWorkflow materializedWorkflow, bool indexTriggers, CancellationToken cancellationToken = default)
    {
        await AssignIdentities(materializedWorkflow.Workflow, cancellationToken);
        var result = await AddOrUpdateAsync(materializedWorkflow, cancellationToken);

        return await result.OnSuccessAsync(async workflowDefinition =>
        {
            if (indexTriggers && workflowDefinition.IsPublished)
                await _triggerIndexer.IndexTriggersAsync(workflowDefinition, cancellationToken);
        });
    }

    private async Task AssignIdentities(Workflow workflow, CancellationToken cancellationToken)
    {
        await _identityGraphService.AssignIdentitiesAsync(workflow, cancellationToken);
    }

    private async Task<Result<WorkflowDefinition>> AddOrUpdateAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var workflowDefinition = await AddOrUpdateCoreAsync(materializedWorkflow, cancellationToken);
            return Result.Success(workflowDefinition);
        }
        catch (Exception e) when (!(e is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            if (e is InvalidOperationException && e.Message.Contains("already exists in another tenant", StringComparison.Ordinal))
            {
                _logger.LogError(e,
                    "Skipping adding workflow {WorkflowId} from provider {Provider} because a record with that Id already exists in another tenant.",
                    materializedWorkflow.Workflow.Identity.DefinitionId,
                    materializedWorkflow.ProviderName);
            }
            else
            {
                _logger.LogError(e, "An error occurred while populating the workflow definition store for workflow {WorkflowId} (Name: {WorkflowName}, Version: {Version})",
                    materializedWorkflow.Workflow.Identity.DefinitionId,
                    materializedWorkflow.Workflow.WorkflowMetadata.Name,
                    materializedWorkflow.Workflow.Identity.Version);
            }

            return Result.Failure<WorkflowDefinition>(e);
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

        var materializerContextJson = GetMaterializerContext(materializedWorkflow);
        var stringData = GetStringData(materializedWorkflow);
        var originalSource = materializedWorkflow.OriginalSource;

        // Check if there's already a workflow definition stored with this definition ID and version.
        var specificVersionFilter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.SpecificVersion(workflow.Identity.Version)
        };

        await RestampClrTenantAsync(materializedWorkflow, cancellationToken);

        var existingDefinitionVersion = await _workflowDefinitionStore.FindAsync(specificVersionFilter, cancellationToken);
        ApplyDeclaredClrTenant(materializedWorkflow, existingDefinitionVersion);

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

        // Determine the tenant ID for the workflow definition
        // If the workflow has no tenant ID, use the current tenant (normalized to handle null -> "*")
        var workflowTenantId = workflow.Identity.TenantId ?? _tenantAccessor.TenantId;

        var workflowDefinition = existingDefinitionVersion ?? new WorkflowDefinition
        {
            DefinitionId = workflow.Identity.DefinitionId,
            Id = workflow.Identity.Id,
            Version = workflow.Identity.Version,
            TenantId = workflowTenantId,
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
        workflowDefinition.StringData = stringData;
        workflowDefinition.OriginalSource = originalSource;
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

        // GetPersistedStates replays these rules to predict the stored IsLatest and IsPublished; keep both in sync.
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

        // GetPersistedStates replays these rules; keep both in sync.
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

    private async Task RestampClrTenantAsync(MaterializedWorkflow materializedWorkflow, CancellationToken cancellationToken)
    {
        if (_clrTenantRestamper == null || !IsClr(materializedWorkflow))
        {
            return;
        }

        var identity = materializedWorkflow.Workflow.Identity;
        try
        {
            await _clrTenantRestamper.RestampAsync(
                identity.DefinitionId,
                identity.Version,
                identity.TenantId ?? Tenant.AgnosticTenantId,
                cancellationToken);
        }
        catch (Exception e) when (!(e is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            _logger.LogError(e,
                "Skipping CLR tenant re-stamp for workflow {WorkflowId} (Version: {Version}). Population continues.",
                identity.DefinitionId,
                identity.Version);
        }
    }

    private static void ApplyDeclaredClrTenant(MaterializedWorkflow materializedWorkflow, WorkflowDefinition? existing)
    {
        if (existing == null || !IsClr(materializedWorkflow) || existing.ProviderName != ClrWorkflowsProvider.ProviderName)
        {
            return;
        }

        var declaredTenantId = materializedWorkflow.Workflow.Identity.TenantId ?? Tenant.AgnosticTenantId;
        if (!string.Equals(existing.TenantId ?? string.Empty, declaredTenantId, StringComparison.Ordinal))
        {
            existing.TenantId = declaredTenantId;
        }
    }

    private static bool IsClr(MaterializedWorkflow materializedWorkflow) =>
        string.Equals(materializedWorkflow.ProviderName, ClrWorkflowsProvider.ProviderName, StringComparison.Ordinal);

    /// <summary>
    /// Syncs the items in the primary list with existing items in the secondary list, even when the object instances are not the same (but their IDs are).
    /// </summary>
    private void SyncExistingCopies(List<WorkflowDefinition> primary, HashSet<WorkflowDefinition> secondary)
    {
        var ids = secondary.Select(x => x.Id).ToHashSet();
        primary.RemoveAll(x => ids.Contains(x.Id));
        primary.AddRange(secondary);
    }
}
