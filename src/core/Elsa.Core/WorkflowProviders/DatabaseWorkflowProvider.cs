using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
    public class DatabaseWorkflowProvider : WorkflowProvider
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;

        public DatabaseWorkflowProvider(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
        }

        protected override async ValueTask<IEnumerable<IWorkflowBlueprint>> OnGetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _workflowDefinitionStore.ListAsync(cancellationToken: cancellationToken);
            return workflowDefinitions.Select(_workflowBlueprintMaterializer.CreateWorkflowBlueprint);
        }
    }
}