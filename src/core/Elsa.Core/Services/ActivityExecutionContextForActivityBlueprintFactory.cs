using System;
using System.Threading;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Default implementation of <see cref="ICreatesActivityExecutionContextForActivityBlueprint"/>.
    /// </summary>
    public class ActivityExecutionContextForActivityBlueprintFactory : ICreatesActivityExecutionContextForActivityBlueprint
    {
        readonly IServiceProvider serviceProvider;

        public ActivityExecutionContextForActivityBlueprintFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        }

        /// <summary>
        /// Creates a activity execution context for the specified activity blueprint.
        /// </summary>
        /// <param name="activityBlueprint">An activity blueprint</param>
        /// <param name="workflowExecutionContext">A workflow execution context</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>An activity execution context</returns>
        public ActivityExecutionContext CreateActivityExecutionContext(IActivityBlueprint activityBlueprint,
                                                                       WorkflowExecutionContext workflowExecutionContext,
                                                                       CancellationToken cancellationToken)
        {
            return new ActivityExecutionContext(serviceProvider, workflowExecutionContext, activityBlueprint, null, false, cancellationToken);
        }
    }
}