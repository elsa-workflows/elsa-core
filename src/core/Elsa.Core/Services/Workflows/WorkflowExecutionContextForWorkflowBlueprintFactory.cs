using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    /// <summary>
    /// Default implementation of <see cref="ICreatesWorkflowExecutionContextForWorkflowBlueprint"/>.
    /// </summary>
    public class WorkflowExecutionContextForWorkflowBlueprintFactory : ICreatesWorkflowExecutionContextForWorkflowBlueprint
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowFactory _workflowFactory;

        public WorkflowExecutionContextForWorkflowBlueprintFactory(IServiceProvider serviceProvider, IWorkflowFactory workflowFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _workflowFactory = workflowFactory ?? throw new ArgumentNullException(nameof(workflowFactory));
        }

        /// <summary>
        /// Creates a workflow execution context for the specified workflow blueprint.
        /// </summary>
        /// <param name="workflowBlueprint">A workflow blueprint</param>
        /// <param name="cancellationToken">An optional cancellation token</param>
        /// <returns>A task for a workflow execution context</returns>
        public async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            return new WorkflowExecutionContext(_serviceProvider, workflowBlueprint, workflowInstance);
        }
    }
}