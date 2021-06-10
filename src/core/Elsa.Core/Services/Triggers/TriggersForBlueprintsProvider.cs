using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Triggers
{
    /// <summary>
    /// Default implementation of <see cref="IGetsTriggersForWorkflowBlueprints"/> which
    /// gets all of the workflow triggers for a collection of workflow blueprints.
    /// </summary>
    public class TriggersForBlueprintsProvider : IGetsTriggersForWorkflowBlueprints
    {
        readonly IActivityTypeService _activityTypeService;
        readonly ICreatesWorkflowExecutionContextForWorkflowBlueprint _workflowExecutionContextFactory;
        readonly IGetsTriggersForActivityBlueprintAndWorkflow _triggerProvider;
        readonly IGetsStartActivities _startingActivitiesProvider;

        public TriggersForBlueprintsProvider(
            IActivityTypeService activityTypeService,
            ICreatesWorkflowExecutionContextForWorkflowBlueprint workflowExecutionContextFactory,
            IGetsTriggersForActivityBlueprintAndWorkflow triggerProvider,
            IGetsStartActivities startingActivitiesProvider)
        {
            _activityTypeService = activityTypeService ?? throw new ArgumentNullException(nameof(activityTypeService));
            _workflowExecutionContextFactory = workflowExecutionContextFactory ?? throw new ArgumentNullException(nameof(workflowExecutionContextFactory));
            _triggerProvider = triggerProvider ?? throw new ArgumentNullException(nameof(triggerProvider));
            _startingActivitiesProvider = startingActivitiesProvider ?? throw new ArgumentNullException(nameof(startingActivitiesProvider));
        }

        /// <summary>
        /// Gets the triggers for all of the specified workflow blueprints.
        /// </summary>
        /// <param name="workflowBlueprints">The workflow blueprints for which to get triggers.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task which exposes an enumerable collection of workflow triggers.</returns>
        public async Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(
            IEnumerable<IWorkflowBlueprint> workflowBlueprints,
            CancellationToken cancellationToken = default)
        {
            var activityTypes = (await _activityTypeService.GetActivityTypesAsync(cancellationToken)).ToDictionary(x => x.TypeName);
            var tasksOfListsOfTriggers = workflowBlueprints.Select(workflowBlueprint => GetWorkflowTriggersForWorkflowBlueprintAsync(workflowBlueprint, activityTypes, cancellationToken));

            return (await Task.WhenAll(tasksOfListsOfTriggers))
                .SelectMany(x => x)
                .ToList();
        }

        private async Task<IList<WorkflowTrigger>> GetWorkflowTriggersForWorkflowBlueprintAsync(
            IWorkflowBlueprint workflowBlueprint,
            IDictionary<string, ActivityType> activityTypes,
            CancellationToken cancellationToken)
        {
            var startingActivityBlueprints = _startingActivitiesProvider.GetStartActivities(workflowBlueprint);
            var workflowExecutionContext = await _workflowExecutionContextFactory.CreateWorkflowExecutionContextAsync(workflowBlueprint, cancellationToken);
            
            var tasksOfCollectionsOfTriggers = startingActivityBlueprints
                .Select(async activityBlueprint => await _triggerProvider.GetTriggersForActivityBlueprintAsync(activityBlueprint,
                    workflowExecutionContext,
                    activityTypes,
                    cancellationToken));
            
            return (await Task.WhenAll(tasksOfCollectionsOfTriggers))
                .SelectMany(x => x)
                .ToList();
        }
    }
}