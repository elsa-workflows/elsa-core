using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Data.Services
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
    public class DatabaseWorkflowProvider : IWorkflowProvider
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;

        public DatabaseWorkflowProvider(IWorkflowDefinitionManager workflowDefinitionManager, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _workflowDefinitionManager.ListAsync(cancellationToken);
            return workflowDefinitions.Select(_workflowBlueprintMaterializer.CreateWorkflowBlueprint);
        }
    }
}