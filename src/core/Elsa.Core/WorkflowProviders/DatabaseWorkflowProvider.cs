using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Repositories;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
    public class DatabaseWorkflowProvider : WorkflowProvider
    {
        private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;

        public DatabaseWorkflowProvider(IWorkflowDefinitionRepository workflowDefinitionRepository, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer)
        {
            _workflowDefinitionRepository = workflowDefinitionRepository;
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
        }

        protected override async ValueTask<IEnumerable<IWorkflowBlueprint>> OnGetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _workflowDefinitionRepository.ListAsync(cancellationToken: cancellationToken);
            return workflowDefinitions.Select(_workflowBlueprintMaterializer.CreateWorkflowBlueprint);
        }
    }
}