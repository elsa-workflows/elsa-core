using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    /// <summary>
    /// Default implementation of <see cref="IGetsTriggersForActivityBlueprintAndWorkflow"/>.
    /// </summary>
    public class TriggersForActivityBlueprintAndWorkflowProvider : IGetsTriggersForActivityBlueprintAndWorkflow
    {
        readonly IBookmarkHasher bookmarkHasher;
        readonly IEnumerable<IBookmarkProvider> bookmarkProviders;
        readonly ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory;

        public TriggersForActivityBlueprintAndWorkflowProvider(
            IBookmarkHasher bookmarkHasher,
            IEnumerable<IBookmarkProvider> bookmarkProviders,
            ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory)
        {
            this.bookmarkHasher = bookmarkHasher ?? throw new System.ArgumentNullException(nameof(bookmarkHasher));
            this.bookmarkProviders = bookmarkProviders ?? throw new System.ArgumentNullException(nameof(bookmarkProviders));
            this.activityExecutionContextFactory = activityExecutionContextFactory ?? throw new System.ArgumentNullException(nameof(activityExecutionContextFactory));
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
            var activityExecutionContext = activityExecutionContextFactory.CreateActivityExecutionContext(
                activity,
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

        async Task<IList<WorkflowTrigger>> GetTriggersForBookmarkProvider(
            IBookmarkProvider provider,
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