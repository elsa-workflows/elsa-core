using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Providers.Workflows
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
    [SkipTriggerIndexing]
    public class DatabaseWorkflowProvider : WorkflowProvider
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;
        private readonly ILogger _logger;

        public DatabaseWorkflowProvider(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer, ILogger<DatabaseWorkflowProvider> logger)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
            _logger = logger;
        }

        public override async IAsyncEnumerable<IWorkflowBlueprint> ListAsync(
            VersionOptions versionOptions,
            int? skip = default,
            int? take = default,
            string? tenantId = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var specification = (ISpecification<WorkflowDefinition>)new VersionOptionsSpecification(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                specification = specification.WithTenant(tenantId);
            
            var paging = skip != null && take != null ? new Paging(skip.Value, take.Value) : default;
            var orderBy = new OrderBy<WorkflowDefinition>(x => x.Id, SortDirection.Ascending);
            var workflowDefinitions = await _workflowDefinitionStore.FindManyAsync(specification, orderBy, paging, cancellationToken);

            foreach (var workflowDefinition in workflowDefinitions)
            {
                var workflowBlueprint = await TryMaterializeBlueprintAsync(workflowDefinition, cancellationToken);

                if (workflowBlueprint != null)
                    yield return workflowBlueprint;
            }
        }

        public override async ValueTask<int> CountAsync(VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var specification = (ISpecification<WorkflowDefinition>)new VersionOptionsSpecification(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                specification = specification.WithTenant(tenantId);

            return await _workflowDefinitionStore.CountAsync(specification, cancellationToken);
        }

        public override async ValueTask<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionIdSpecification(definitionId, versionOptions), cancellationToken);
            return workflowDefinition == null ? null : await TryMaterializeBlueprintAsync(workflowDefinition, cancellationToken);
        }

        private async Task<IWorkflowBlueprint?> TryMaterializeBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            try
            {
                return await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to materialize workflow definition {WorkflowDefinitionId} with version {WorkflowDefinitionVersion}", workflowDefinition.DefinitionId, workflowDefinition.Version);
            }

            return null;
        }
    }
}