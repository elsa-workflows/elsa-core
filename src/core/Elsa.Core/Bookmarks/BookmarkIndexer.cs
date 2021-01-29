﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Bookmarks;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using Rebus.Extensions;

namespace Elsa.Bookmarks
{
    public class BookmarkIndexer : IBookmarkIndexer
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IEnumerable<IBookmarkProvider> _providers;
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
            await DeleteBookmarksAsync(workflowInstanceIds, cancellationToken);

            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToDictionaryAsync(x => (x.Id, x.Version), cancellationToken);
            var entities = new List<Bookmark>();
            
            foreach (var workflowInstance in workflowInstanceList.Where(x => x.WorkflowStatus == WorkflowStatus.Suspended))
            {
                var workflowBlueprintKey = (workflowInstance.DefinitionId, workflowInstance.Version);
                var workflowBlueprint = workflowBlueprints.ContainsKey(workflowBlueprintKey) ? workflowBlueprints[workflowBlueprintKey] : default;

                if (workflowBlueprint == null)
                {
                    _logger.LogWarning("Could not find workflow definition for workflow {WorkflowInstanceId}", workflowInstance.Id);
                    return;
                }

                var blockingActivities = workflowBlueprint.GetBlockingActivities(workflowInstance!);
                var bookmarkedWorkflows = await ExtractBookmarksAsync(workflowBlueprint, workflowInstance, blockingActivities, true, cancellationToken).ToList();
                var bookmarks = MapBookmarks(bookmarkedWorkflows, workflowInstance);
                entities.AddRange(bookmarks);
            }
            
            await _bookmarkStore.AddManyAsync(entities, cancellationToken);

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
        
        private IEnumerable<Bookmark> MapBookmarks(IEnumerable<BookmarkedWorkflow> bookmarkedWorkflows, WorkflowInstance workflowInstance) =>
            bookmarkedWorkflows.SelectMany(triggerDescriptor => triggerDescriptor.Bookmarks.Select(x => new Bookmark
            {
                Id = _idGenerator.Generate(),
                TenantId = workflowInstance.TenantId,
                ActivityType = triggerDescriptor.ActivityType,
                ActivityId = triggerDescriptor.ActivityId,
                WorkflowInstanceId = workflowInstance.Id,
                Hash = _hasher.Hash(x),
                Model = _contentSerializer.Serialize(x),
                ModelType = x.GetType().GetSimpleAssemblyQualifiedName()
            }));

        private async Task<IEnumerable<BookmarkedWorkflow>> ExtractBookmarksAsync(
            IWorkflowBlueprint workflowBlueprint,
            WorkflowInstance workflowInstance,
            IEnumerable<IActivityBlueprint> blockingActivities,
            bool loadContext,
            CancellationToken cancellationToken)
        {
            // Setup workflow execution context
            var scope = _serviceProvider.CreateScope();
            var workflowExecutionContext = new WorkflowExecutionContext(scope, workflowBlueprint, workflowInstance);

            // Load workflow context.
            workflowExecutionContext.WorkflowContext =
                loadContext &&
                workflowBlueprint.ContextOptions != null &&
                !string.IsNullOrWhiteSpace(workflowInstance.ContextId)
                    ? await _workflowContextManager.LoadContext(new LoadWorkflowContext(workflowExecutionContext), cancellationToken)
                    : default;

            // Extract bookmarks for each blocking activity.
            var bookmarkedWorkflows = new List<BookmarkedWorkflow>();

            foreach (var blockingActivity in blockingActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, blockingActivity, null, cancellationToken);
                var providerContext = new BookmarkProviderContext(activityExecutionContext, BookmarkIndexingMode.WorkflowInstance);
                var providers = _providers.Where(x => x.ForActivityType == blockingActivity.Type);

                foreach (var provider in providers)
                {
                    var bookmarks = (await provider.GetBookmarksAsync(providerContext, cancellationToken)).ToList();

                    var bookmarkedWorkflow = new BookmarkedWorkflow
                    {
                        WorkflowBlueprint = workflowBlueprint,
                        WorkflowInstanceId = workflowInstance.Id,
                        ActivityType = blockingActivity.Type,
                        ActivityId = blockingActivity.Id,
                        Bookmarks = bookmarks
                    };

                    bookmarkedWorkflows.Add(bookmarkedWorkflow);
                }
            }

            return bookmarkedWorkflows;
        }
    }
}