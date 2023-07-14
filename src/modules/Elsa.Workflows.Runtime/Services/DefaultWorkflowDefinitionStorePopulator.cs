using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Medallion.Threading;
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
    private readonly IDistributedLockProvider _distributedLockProvider;
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

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = workflow.Identity.DefinitionId,
            VersionOptions = VersionOptions.SpecificVersion(workflow.Version)
        };

        // Serialize materializer context.
        var materializerContext = materializedWorkflow.MaterializerContext;
        var materializerContextJson = materializerContext != null ? _payloadSerializer.Serialize(materializerContext) : default;

        // Serialize the workflow root.
        var workflowJson = _activitySerializer.Serialize(workflow.Root);

        // Check if there's already a workflow definition stored with this workflow.
        var existingDefinition = await _workflowDefinitionStore.FindAsync(filter, cancellationToken) ?? new WorkflowDefinition
        {
            DefinitionId = workflow.Identity.DefinitionId,
            Id = workflow.Identity.Id,
            Version = workflow.Identity.Version
        };

        existingDefinition.Description = workflow.WorkflowMetadata.Description;
        existingDefinition.Name = workflow.WorkflowMetadata.Name;
        existingDefinition.IsLatest = workflow.Publication.IsLatest;
        existingDefinition.IsPublished = workflow.Publication.IsPublished;
        existingDefinition.IsReadonly = workflow.IsReadonly;
        existingDefinition.CustomProperties = workflow.CustomProperties;
        existingDefinition.Variables = workflow.Variables;
        existingDefinition.Inputs = workflow.Inputs;
        existingDefinition.Outputs = workflow.Outputs;
        existingDefinition.Outcomes = workflow.Outcomes;
        existingDefinition.StringData = workflowJson;
        existingDefinition.CreatedAt = workflow.WorkflowMetadata.CreatedAt == default ? _systemClock.UtcNow : workflow.WorkflowMetadata.CreatedAt;
        existingDefinition.Options = workflow.Options;
        existingDefinition.ProviderName = materializedWorkflow.ProviderName;
        existingDefinition.MaterializerContext = materializerContextJson;
        existingDefinition.MaterializerName = materializedWorkflow.MaterializerName;

        await _workflowDefinitionStore.SaveAsync(existingDefinition, cancellationToken);
    }

    private async Task IndexTriggersAsync(MaterializedWorkflow workflow, CancellationToken cancellationToken) => await _triggerIndexer.IndexTriggersAsync(workflow.Workflow, cancellationToken);
}