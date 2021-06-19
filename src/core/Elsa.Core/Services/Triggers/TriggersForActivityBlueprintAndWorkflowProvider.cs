using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;
using Elsa.Services.Models;

namespace Elsa.Services.Triggers
{
    /// <summary>
    /// Default implementation of <see cref="IGetsTriggersForActivityBlueprintAndWorkflow"/>.
    /// </summary>
    public class TriggersForActivityBlueprintAndWorkflowProvider : IGetsTriggersForActivityBlueprintAndWorkflow
    {
        private readonly IBookmarkHasher _bookmarkHasher;
        private readonly IEnumerable<IBookmarkProvider> _bookmarkProviders;
        private readonly ICreatesActivityExecutionContextForActivityBlueprint _activityExecutionContextFactory;

        public TriggersForActivityBlueprintAndWorkflowProvider(
            IBookmarkHasher bookmarkHasher,
            IEnumerable<IBookmarkProvider> bookmarkProviders,
            ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory)
        {
            _bookmarkHasher = bookmarkHasher;
            _bookmarkProviders = bookmarkProviders;
            _activityExecutionContextFactory = activityExecutionContextFactory;
        }

        /// <summary>
        /// Gets a collection of the workflow triggers for the specified activity blueprint.
        /// </summary>
        /// <param name="activityBlueprint">An activity blueprint</param>
        /// <param name="workflowExecutionContext">A workflow execution context</param>
        /// <param name="activityTypes">A dictionary of all of the activity types (by name)</param>
        /// <param name="cancellationToken">An optional cancellation token</param>
        /// <returns>A task exposing a collection of workflow triggers for the activity and workflow.</returns>
        public async Task<IEnumerable<WorkflowTrigger>> GetTriggersForActivityBlueprintAsync(
            IActivityBlueprint activityBlueprint,
            WorkflowExecutionContext workflowExecutionContext,
            IDictionary<string, ActivityType> activityTypes,
            CancellationToken cancellationToken = default)
        {
            var bookmarkProviderContext = GetBookmarkProviderContext(activityBlueprint, workflowExecutionContext, cancellationToken, activityTypes);
            var supportedBookmarkProviders = await GetSupportedBookmarkProvidersForContextAsync(bookmarkProviderContext)
                .ToListAsync(cancellationToken);

            var tasksOfListsOfTriggers = supportedBookmarkProviders
                .Select(bookmarkProvider => GetTriggersForBookmarkProvider(bookmarkProvider,
                    bookmarkProviderContext,
                    activityBlueprint,
                    workflowExecutionContext.WorkflowBlueprint,
                    cancellationToken));
            return (await Task.WhenAll(tasksOfListsOfTriggers))
                .SelectMany(x => x)
                .ToList();
        }

        private BookmarkProviderContext GetBookmarkProviderContext(
            IActivityBlueprint activity,
            WorkflowExecutionContext workflowExecutionContext,
            CancellationToken cancellationToken,
            IDictionary<string, ActivityType> activityTypes)
        {
            var activityExecutionContext = _activityExecutionContextFactory.CreateActivityExecutionContext(
                activity,
                workflowExecutionContext,
                cancellationToken);

            var activityType = activityTypes[activity.Type];
            return new BookmarkProviderContext(activityExecutionContext, activityType, BookmarkIndexingMode.WorkflowBlueprint);
        }

        private async IAsyncEnumerable<IBookmarkProvider> GetSupportedBookmarkProvidersForContextAsync(BookmarkProviderContext context)
        {
            foreach (var provider in _bookmarkProviders)
                if (await provider.SupportsActivityAsync(context))
                    yield return provider;
        }

        private async Task<IList<WorkflowTrigger>> GetTriggersForBookmarkProvider(
            IBookmarkProvider provider,
            BookmarkProviderContext context,
            IActivityBlueprint activityBlueprint,
            IWorkflowBlueprint workflowBlueprint,
            CancellationToken cancellationToken = default)
        {
            var bookmarkResults = (await provider.GetBookmarksAsync(context, cancellationToken)).ToList();
            return bookmarkResults
                .Select(x => new WorkflowTrigger(workflowBlueprint, activityBlueprint.Id, x.ActivityTypeName ?? activityBlueprint.Type, _bookmarkHasher.Hash(x.Bookmark), x.Bookmark))
                .ToList();
        }
    }
}