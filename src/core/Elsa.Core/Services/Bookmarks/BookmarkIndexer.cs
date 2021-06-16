using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Bookmarks;
using Elsa.Serialization;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Rebus.Extensions;

namespace Elsa.Services.Bookmarks
{
    public class BookmarkIndexer : IBookmarkIndexer
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IEnumerable<IBookmarkProvider> _providers;
        private readonly IActivityTypeService _activityTypeService;
        private readonly IIdGenerator _idGenerator;
        private readonly IContentSerializer _contentSerializer;
        private readonly IBookmarkHasher _hasher;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public BookmarkIndexer(
            IWorkflowRegistry workflowRegistry,
            IBookmarkStore bookmarkStore,
            IWorkflowContextManager workflowContextManager,
            IEnumerable<IBookmarkProvider> providers,
            IActivityTypeService activityTypeService,
            IIdGenerator idGenerator,
            IContentSerializer contentSerializer,
            IServiceProvider serviceProvider,
            IBookmarkHasher hasher,
            ILogger<BookmarkIndexer> logger)
        {
            _workflowRegistry = workflowRegistry;
            _bookmarkStore = bookmarkStore;
            _workflowContextManager = workflowContextManager;
            _providers = providers;
            _activityTypeService = activityTypeService;
            _idGenerator = idGenerator;
            _contentSerializer = contentSerializer;
            _serviceProvider = serviceProvider;
            _hasher = hasher;
            _logger = logger;
        }

        public async Task IndexBookmarksAsync(IEnumerable<WorkflowInstance> workflowInstances, CancellationToken cancellationToken)
        {
            _stopwatch.Restart();
            _logger.LogInformation("Indexing bookmarks");

            var workflowInstanceList = workflowInstances.ToList();
            var workflowInstanceIds = workflowInstanceList.Select(x => x.Id).ToList();
            var oldBookmarks = await FindBookmarksAsync(workflowInstanceIds, cancellationToken).ToList();
            var workflowBlueprints = await _workflowRegistry.ListActiveAsync(cancellationToken);
            var workflowBlueprintsDictionary = workflowBlueprints.ToDictionary(x => (x.Id, x.Version));
            var entities = new List<Bookmark>();
            
            foreach (var workflowInstance in workflowInstanceList.Where(x => x.WorkflowStatus == WorkflowStatus.Suspended))
            {
                var workflowBlueprintKey = (workflowInstance.DefinitionId, workflowInstance.Version);
                var workflowBlueprint = workflowBlueprintsDictionary.ContainsKey(workflowBlueprintKey) ? workflowBlueprintsDictionary[workflowBlueprintKey] : default;

                if (workflowBlueprint == null)
                {
                    _logger.LogWarning("Could not find workflow definition for workflow {WorkflowInstanceId}", workflowInstance.Id);
                    return;
                }

                var blockingActivities = workflowBlueprint.GetBlockingActivities(workflowInstance!);
                var bookmarkedWorkflows = await ExtractBookmarksAsync(workflowBlueprint, workflowInstance, blockingActivities, cancellationToken).ToList();
                var bookmarks = MapBookmarks(bookmarkedWorkflows, workflowInstance);
                entities.AddRange(bookmarks);
            }
            
            await _bookmarkStore.AddManyAsync(entities, cancellationToken);
            var oldBookmarkIds = oldBookmarks.Select(x => x.Id).ToList();
            await _bookmarkStore.DeleteManyAsync(new BookmarkIdsSpecification(oldBookmarkIds), cancellationToken);

            _stopwatch.Stop();
            _logger.LogInformation("Indexed {BookmarkCount} bookmarks in {ElapsedTime}", entities.Count, _stopwatch.Elapsed);
        }
        
        public async Task IndexBookmarksAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken) => await IndexBookmarksAsync(new[] { workflowInstance }, cancellationToken);

        public async Task DeleteBookmarksAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _bookmarkStore.DeleteManyAsync(specification, cancellationToken);
        }

        public async Task DeleteBookmarksAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdSpecification(workflowInstanceId);
            var count = await _bookmarkStore.DeleteManyAsync(specification, cancellationToken);
            
            _logger.LogDebug("Deleted {DeletedBookmarkCount} bookmarks for workflow {WorkflowInstanceId}", count, workflowInstanceId);
        }
        
        private async Task<IEnumerable<Bookmark>> FindBookmarksAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            return await _bookmarkStore.FindManyAsync(specification, cancellationToken: cancellationToken);
        }
        
        private IEnumerable<Bookmark> MapBookmarks(IEnumerable<BookmarkedWorkflow> bookmarkedWorkflows, WorkflowInstance workflowInstance) =>
            bookmarkedWorkflows.Select(x => new Bookmark
            {
                Id = _idGenerator.Generate(),
                TenantId = workflowInstance.TenantId,
                ActivityType = x.ActivityType,
                ActivityId = x.ActivityId,
                WorkflowInstanceId = workflowInstance.Id,
                CorrelationId = workflowInstance.CorrelationId,
                Hash = _hasher.Hash(x.Bookmark),
                Model = _contentSerializer.Serialize(x.Bookmark),
                ModelType = x.Bookmark.GetType().GetSimpleAssemblyQualifiedName()
            });

        private async Task<IEnumerable<BookmarkedWorkflow>> ExtractBookmarksAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            IEnumerable<IActivityBlueprint> blockingActivities,
            CancellationToken cancellationToken)
        {
            // Setup workflow execution context
            var workflowExecutionContext = new WorkflowExecutionContext(_serviceProvider, workflowBlueprint, workflowInstance);

            // Extract bookmarks for each blocking activity.
            var bookmarkedWorkflows = new List<BookmarkedWorkflow>();

            var activityTypes = (await _activityTypeService.GetActivityTypesAsync(cancellationToken)).ToDictionary(x => x.TypeName);

            foreach (var blockingActivity in blockingActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(_serviceProvider, workflowExecutionContext, blockingActivity, null, false, cancellationToken);
                var activityType = activityTypes[blockingActivity.Type];
                var providerContext = new BookmarkProviderContext(activityExecutionContext, activityType, BookmarkIndexingMode.WorkflowInstance);
                var providers = await FilterProvidersAsync(providerContext).ToListAsync(cancellationToken);

                foreach (var provider in providers)
                {
                    var bookmarkResults = (await provider.GetBookmarksAsync(providerContext, cancellationToken)).ToList();

                    bookmarkedWorkflows.AddRange(bookmarkResults.Select(bookmarkResult => new BookmarkedWorkflow
                    {
                        WorkflowBlueprint = workflowBlueprint,
                        WorkflowInstanceId = workflowInstance.Id,
                        ActivityType = bookmarkResult.ActivityTypeName ?? blockingActivity.Type,
                        ActivityId = blockingActivity.Id,
                        Bookmark = bookmarkResult.Bookmark
                    }));
                }
            }

            return bookmarkedWorkflows;
        }
        
        private async IAsyncEnumerable<IBookmarkProvider> FilterProvidersAsync(BookmarkProviderContext context)
        {
            foreach (var provider in _providers)
                if (await provider.SupportsActivityAsync(context))
                    yield return provider;
        }
    }
}