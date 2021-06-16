using System.Threading;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// An object which can create an activity execution context for a specified activity blueprint.
    /// </summary>
    public interface ICreatesActivityExecutionContextForActivityBlueprint
    {
        /// <summary>
        /// Creates a activity execution context for the specified activity blueprint.
        /// </summary>
        /// <param name="activityBlueprint">An activity blueprint</param>
        /// <param name="workflowExecutionContext">A workflow execution context</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>An activity execution context</returns>
        ActivityExecutionContext CreateActivityExecutionContext(IActivityBlueprint activityBlueprint,
                                                                WorkflowExecutionContext workflowExecutionContext,
                                                                CancellationToken cancellationToken);
    }
}