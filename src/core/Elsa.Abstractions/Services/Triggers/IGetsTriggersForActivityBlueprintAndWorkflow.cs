using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// An object which can get a collection of the <see cref="WorkflowTrigger"/> for a specified activity blueprint and workflow.
    /// </summary>
    public interface IGetsTriggersForActivityBlueprintAndWorkflow
    {
        /// <summary>
        /// Gets a collection of the workflow triggers for the specified activity blueprint.
        /// </summary>
        /// <param name="activityBlueprint">An activity blueprint</param>
        /// <param name="workflowExecutionContext">A workflow execution context</param>
        /// <param name="activityTypes">A dictionary of all of the activity types (by name)</param>
        /// <param name="cancellationToken">An optional cancellation token</param>
        /// <returns>A task exposing a collection of workflow triggers for the activity and workflow.</returns>
        Task<IEnumerable<WorkflowTrigger>> GetTriggersForActivityBlueprintAsync(IActivityBlueprint activityBlueprint,
                                                                                WorkflowExecutionContext workflowExecutionContext,
                                                                                IDictionary<string, ActivityType> activityTypes,
                                                                                CancellationToken cancellationToken = default);
    }
}