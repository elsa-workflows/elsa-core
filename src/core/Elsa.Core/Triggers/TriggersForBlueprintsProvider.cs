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
        readonly IActivityTypeService activityTypeService;
        readonly IBookmarkHasher bookmarkHasher;
        readonly IEnumerable<IBookmarkProvider> bookmarkProviders;
        readonly ICreatesWorkflowExecutionContextForWorkflowBlueprint workflowExecutionContextFactory;
        readonly ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory;

        public TriggersForBlueprintsProvider(IActivityTypeService activityTypeService,
                                             IBookmarkHasher bookmarkHasher,
                                             IEnumerable<IBookmarkProvider> bookmarkProviders,
                                             ICreatesWorkflowExecutionContextForWorkflowBlueprint workflowExecutionContextFactory,
                                             ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory)
        {
            this.activityTypeService = activityTypeService ?? throw new ArgumentNullException(nameof(activityTypeService));
            this.bookmarkHasher = bookmarkHasher ?? throw new ArgumentNullException(nameof(bookmarkHasher));
            this.bookmarkProviders = bookmarkProviders ?? throw new ArgumentNullException(nameof(bookmarkProviders));
            this.workflowExecutionContextFactory = workflowExecutionContextFactory ?? throw new ArgumentNullException(nameof(workflowExecutionContextFactory));
            this.activityExecutionContextFactory = activityExecutionContextFactory ?? throw new ArgumentNullException(nameof(activityExecutionContextFactory));
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

                foreach (var activityBlueprint in startingActivityBlueprints)
                {
                    var bookmarkProviderContext = GetBookmarkProviderContext(activityBlueprint, workflowExecutionContext, cancellationToken, activityTypes);
                    var supportedBookmarkProviders = await GetSupportedBookmarkProvidersForContextAsync(bookmarkProviderContext).ToListAsync(cancellationToken);

                    foreach (var bookmarkProvider in supportedBookmarkProviders)
                    {
                        var triggers = await GetTriggersForBookmarkProvider(bookmarkProvider,
                                                                            bookmarkProviderContext,
                                                                            activityBlueprint,
                                                                            workflowBlueprint,
                                                                            cancellationToken);
                        allTriggers.AddRange(triggers);
                    }
                }
            }

            return allTriggers;
        }

        BookmarkProviderContext GetBookmarkProviderContext(IActivityBlueprint activity,
                                                           WorkflowExecutionContext workflowExecutionContext,
                                                           CancellationToken cancellationToken,
                                                           IDictionary<string,ActivityType> activityTypes)
        {
            var activityExecutionContext = activityExecutionContextFactory.CreateActivityExecutionContext(activity,
                                                                                                          workflowExecutionContext,
                                                                                                          cancellationToken);
            var activityType = activityTypes[activity.Type];
            return new BookmarkProviderContext(activityExecutionContext, activityType, BookmarkIndexingMode.WorkflowBlueprint);
        }

        async IAsyncEnumerable<IBookmarkProvider> GetSupportedBookmarkProvidersForContextAsync(BookmarkProviderContext context)
        {
            foreach (var provider in bookmarkProviders)
                if (await provider.SupportsActivityAsync(context))
                    yield return provider;
        }

        async Task<IList<WorkflowTrigger>> GetTriggersForBookmarkProvider(IBookmarkProvider provider,
                                                                          BookmarkProviderContext context,
                                                                          IActivityBlueprint activityBlueprint,
                                                                          IWorkflowBlueprint workflowBlueprint,
                                                                          CancellationToken cancellationToken = default)
        {
            var bookmarks = (await provider.GetBookmarksAsync(context, cancellationToken)).ToList();
            return bookmarks
                .Select(x => new WorkflowTrigger(workflowBlueprint, activityBlueprint.Id, activityBlueprint.Type, bookmarkHasher.Hash(x), x))
                .ToList();
        }
    }
}