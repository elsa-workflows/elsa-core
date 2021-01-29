using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Triggers
{
    public class TriggerIndexer : ITriggerIndexer
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IBookmarkHasher _bookmarkHasher;
        private readonly IEnumerable<IBookmarkProvider> _providers;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly ITriggerStore _triggerStore;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch = new();

        public TriggerIndexer(
            IWorkflowRegistry workflowRegistry,
            IBookmarkHasher bookmarkHasher,
            IEnumerable<IBookmarkProvider> providers,
            IServiceScopeFactory serviceScopeFactory,
            IWorkflowFactory workflowFactory,
            ITriggerStore triggerStore,
            ILogger<TriggerIndexer> logger)
        {
            _workflowRegistry = workflowRegistry;
            _bookmarkHasher = bookmarkHasher;
            _providers = providers;
            _serviceScopeFactory = serviceScopeFactory;
            _workflowFactory = workflowFactory;
            _triggerStore = triggerStore;
            _logger = logger;
        }

        public async Task IndexTriggersAsync(CancellationToken cancellationToken = default)
        {
            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
            await IndexTriggersAsync(workflowBlueprints, cancellationToken);
        }

        private async Task IndexTriggersAsync(IEnumerable<IWorkflowBlueprint> workflowBlueprints, CancellationToken cancellationToken = default)
        {
            _stopwatch.Restart();
            _logger.LogInformation("Indexing triggers");

            var workflowBlueprintList = workflowBlueprints.ToList();
            var triggers = (await GetTriggersAsync(workflowBlueprintList, cancellationToken)).ToList();
            
            _stopwatch.Stop();
            _logger.LogInformation("Indexed {TriggerCount} triggers in {ElapsedTime}", triggers.Count, _stopwatch.Elapsed);

            await _triggerStore.StoreAsync(triggers, cancellationToken);
        }
        
        private async Task<IEnumerable<WorkflowTrigger>> GetTriggersAsync(ICollection<IWorkflowBlueprint> workflowBlueprints, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var allTriggers = new List<WorkflowTrigger>();
            
            foreach (var provider in _providers)
            {
                var activityType = provider.ForActivityType;

                foreach (var workflowBlueprint in workflowBlueprints)
                {
                    var startActivities = workflowBlueprint.GetStartActivities(activityType);
                    var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
                    var workflowExecutionContext = new WorkflowExecutionContext(scope, workflowBlueprint, workflowInstance);
                    
                    foreach (var activity in startActivities)
                    {
                        var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, activity, null, cancellationToken);
                        var context = new BookmarkProviderContext(activityExecutionContext, BookmarkIndexingMode.WorkflowBlueprint);
                        var bookmarks = (await provider.GetBookmarksAsync(context, cancellationToken)).ToList();
                        var triggers = bookmarks.Select(x => new WorkflowTrigger(workflowBlueprint, activity.Id, activity.Type, _bookmarkHasher.Hash(x), x)).ToList();
                        allTriggers.AddRange(triggers);
                    }    
                }
            }
            
            return allTriggers;
        }
    }
}