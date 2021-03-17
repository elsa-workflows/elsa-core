using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Bookmarks;
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
        private readonly IActivityTypeService _activityTypeService;
        private readonly IBookmarkHasher _bookmarkHasher;
        private readonly IEnumerable<IBookmarkProvider> _providers;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowFactory _workflowFactory;

        public TriggersForBlueprintsProvider(IActivityTypeService activityTypeService,
                                             IBookmarkHasher bookmarkHasher,
                                             IEnumerable<IBookmarkProvider> providers,
                                             IServiceProvider serviceProvider,
                                             IWorkflowFactory workflowFactory)
        {
            _activityTypeService = activityTypeService;
            _bookmarkHasher = bookmarkHasher;
            _providers = providers;
            _serviceProvider = serviceProvider;
            _workflowFactory = workflowFactory;
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
            var activityTypes = (await _activityTypeService.GetActivityTypesAsync(cancellationToken)).ToDictionary(x => x.TypeName);

            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var startActivities = workflowBlueprint.GetStartActivities();
                var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
                var workflowExecutionContext = new WorkflowExecutionContext(_serviceProvider, workflowBlueprint, workflowInstance);

                foreach (var activity in startActivities)
                {
                    var activityExecutionContext = new ActivityExecutionContext(_serviceProvider, workflowExecutionContext, activity, null, false, cancellationToken);
                    var activityType = activityTypes[activity.Type];
                    var context = new BookmarkProviderContext(activityExecutionContext, activityType, BookmarkIndexingMode.WorkflowBlueprint);
                    var providers = await FilterProvidersAsync(context).ToListAsync(cancellationToken);

                    foreach (var provider in providers)
                    {
                        var bookmarks = (await provider.GetBookmarksAsync(context, cancellationToken)).ToList();
                        var triggers = bookmarks.Select(x => new WorkflowTrigger(workflowBlueprint, activity.Id, activity.Type, _bookmarkHasher.Hash(x), x)).ToList();
                        allTriggers.AddRange(triggers);
                    }
                }
            }

            return allTriggers;
        }

        async IAsyncEnumerable<IBookmarkProvider> FilterProvidersAsync(BookmarkProviderContext context)
        {
            foreach (var provider in _providers)
                if (await provider.SupportsActivityAsync(context))
                    yield return provider;
        }
    }
}