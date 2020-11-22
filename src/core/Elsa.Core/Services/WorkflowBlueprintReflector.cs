using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowBlueprintReflector : IWorkflowBlueprintReflector
    {
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IServiceProvider _serviceProvider;

        public WorkflowBlueprintReflector(IWorkflowFactory workflowFactory, IServiceProvider serviceProvider)
        {
            _workflowFactory = workflowFactory;
            _serviceProvider = serviceProvider;
        }
        
        public async Task<IWorkflowBlueprintWrapper> ReflectAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            var workflowExecutionContext = new WorkflowExecutionContext(_serviceProvider, workflowBlueprint, workflowInstance);
            return new WorkflowBlueprintWrapper(workflowBlueprint, workflowExecutionContext);
        }
    }
}