using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Default implementation of <see cref="ICreatesWorkflowExecutionContextForWorkflowBlueprint"/>.
    /// </summary>
    public class WorkflowExecutionContextForWorkflowBlueprintFactory : ICreatesWorkflowExecutionContextForWorkflowBlueprint
    {
        readonly IServiceProvider serviceProvider;
        readonly IWorkflowFactory workflowFactory;

        public WorkflowExecutionContextForWorkflowBlueprintFactory(IServiceProvider serviceProvider, IWorkflowFactory workflowFactory)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.workflowFactory = workflowFactory ?? throw new ArgumentNullException(nameof(workflowFactory));
        }

        /// <summary>
        /// Creates a workflow execution context for the specified workflow blueprint.
        /// </summary>
        /// <param name="workflowBlueprint">A workflow blueprint</param>
        /// <param name="cancellationToken">An optional cancellation token</param>
        /// <returns>A task for a workflow execution context</returns>
        public async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            return new WorkflowExecutionContext(serviceProvider, workflowBlueprint, workflowInstance);
        }
    }
}