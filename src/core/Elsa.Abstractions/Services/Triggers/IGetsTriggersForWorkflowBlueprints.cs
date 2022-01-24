using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// An object which can get all of the workflow triggers for a collection of workflow blueprints.
    /// </summary>
    public interface IGetsTriggersForWorkflowBlueprints
    {
        /// <summary>
        /// Gets the triggers for all of the specified workflow blueprints.
        /// </summary>
        /// <param name="workflowBlueprints">The workflow blueprints for which to get triggers.</param>
        Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the triggers for the specified workflow blueprint.
        /// </summary>
        /// <param name="workflowBlueprints">The workflow blueprint for which to get triggers.</param>
        Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default);
    }
}