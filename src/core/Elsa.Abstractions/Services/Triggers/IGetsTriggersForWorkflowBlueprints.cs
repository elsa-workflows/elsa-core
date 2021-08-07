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
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task which exposes an enumerable collection of workflow triggers.</returns>
        Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints,
                                                            CancellationToken cancellationToken = default);
    }
}