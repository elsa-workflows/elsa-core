using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Providers.Workflows
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
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

        public override async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _workflowDefinitionStore.FindManyAsync(Specification<WorkflowDefinition>.Identity, cancellationToken: cancellationToken);

            foreach (var workflowDefinition in workflowDefinitions)
            {
                var workflowBlueprint = await TryMaterializeBlueprintAsync(workflowDefinition, cancellationToken);

                if (workflowBlueprint != null)
                    yield return workflowBlueprint;
            }
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