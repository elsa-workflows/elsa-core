using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    public class WorkflowBlueprintReflector : IWorkflowBlueprintReflector
    {
        private readonly IWorkflowFactory _workflowFactory;

        public WorkflowBlueprintReflector(IWorkflowFactory workflowFactory)
        {
            _workflowFactory = workflowFactory;
        }
        
        public async Task<IWorkflowBlueprintWrapper> ReflectAsync(IServiceScope serviceScope, IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            var workflowExecutionContext = new WorkflowExecutionContext(serviceScope, workflowBlueprint, workflowInstance);
            return new WorkflowBlueprintWrapper(workflowBlueprint, workflowExecutionContext);
        }
    }
}