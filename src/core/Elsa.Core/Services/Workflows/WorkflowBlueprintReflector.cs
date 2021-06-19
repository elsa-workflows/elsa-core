using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    public class WorkflowBlueprintReflector : IWorkflowBlueprintReflector
    {
        private readonly IWorkflowFactory _workflowFactory;

        public WorkflowBlueprintReflector(IWorkflowFactory workflowFactory)
        {
            _workflowFactory = workflowFactory;
        }
        
        public async Task<IWorkflowBlueprintWrapper> ReflectAsync(IServiceProvider serviceProvider, IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            var workflowExecutionContext = new WorkflowExecutionContext(serviceProvider, workflowBlueprint, workflowInstance);
            return new WorkflowBlueprintWrapper(workflowBlueprint, workflowExecutionContext);
        }
    }
}