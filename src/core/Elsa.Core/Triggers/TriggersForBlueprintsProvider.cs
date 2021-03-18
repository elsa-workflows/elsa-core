using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    /// <summary>
    /// Default implementation of <see cref="IGetsTriggersForWorkflowBlueprints"/> which
    /// gets all of the workflow triggers for a collection of workflow blueprints.
    /// </summary>
    public class TriggersForBlueprintsProvider : IGetsTriggersForWorkflowBlueprints
    {
        readonly IActivityTypeService activityTypeService;
        readonly ICreatesWorkflowExecutionContextForWorkflowBlueprint workflowExecutionContextFactory;
        readonly IGetsTriggersForActivityBlueprintAndWorkflow triggerProvider;

        public TriggersForBlueprintsProvider(IActivityTypeService activityTypeService,
                                             ICreatesWorkflowExecutionContextForWorkflowBlueprint workflowExecutionContextFactory,
                                             IGetsTriggersForActivityBlueprintAndWorkflow triggerProvider)
        {
            this.activityTypeService = activityTypeService ?? throw new ArgumentNullException(nameof(activityTypeService));
            this.workflowExecutionContextFactory = workflowExecutionContextFactory ?? throw new ArgumentNullException(nameof(workflowExecutionContextFactory));
            this.triggerProvider = triggerProvider ?? throw new ArgumentNullException(nameof(triggerProvider));
        }

        /// <summary>
        /// Gets the triggers for all of the specified workflow blueprints.
        /// </summary>
        /// <param name="workflowBlueprints">The workflow blueprints for which to get triggers.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task which exposes an enumerable collection of workflow triggers.</returns>
        public async Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints,
                                                                         CancellationToken cancellationToken = default)
        {
            var allTriggers = new List<WorkflowTrigger>();
            var activityTypes = (await activityTypeService.GetActivityTypesAsync(cancellationToken)).ToDictionary(x => x.TypeName);

            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var startingActivityBlueprints = workflowBlueprint.GetStartActivities();
                var workflowExecutionContext = await workflowExecutionContextFactory.CreateWorkflowExecutionContextAsync(workflowBlueprint,
                                                                                                                         cancellationToken);
                var tasksOfCollectionsOfTriggers = startingActivityBlueprints
                    .Select(async activityBlueprint => await triggerProvider.GetTriggersForActivityBlueprintAsync(activityBlueprint,
                                                                                                                  workflowExecutionContext,
                                                                                                                  activityTypes,
                                                                                                                  cancellationToken));
                var triggers = (await Task.WhenAll(tasksOfCollectionsOfTriggers))
                    .SelectMany(x => x)
                    .ToList();
                allTriggers.AddRange(triggers);
            }

            return allTriggers;
        }
    }
}