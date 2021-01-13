using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Triggers
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private const string CacheKey = "WorkflowSelector:Triggers";
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IEnumerable<ITriggerProvider> _triggerProviders;
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WorkflowSelector> _logger;
        private IDictionary<string, ICollection<TriggerDescriptor>>? _descriptors;
        private readonly Stopwatch _stopwatch = new();

        public WorkflowSelector(
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowContextManager workflowContextManager,
            IEnumerable<ITriggerProvider> triggerProviders,
            IMemoryCache memoryCache,
            IServiceProvider serviceProvider,
            ILogger<WorkflowSelector> logger)
        {
            _workflowRegistry = workflowRegistry;
            _workflowFactory = workflowFactory;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowContextManager = workflowContextManager;
            _triggerProviders = triggerProviders;
            _memoryCache = memoryCache;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default)
        {
            _descriptors ??= await GetDescriptorsAsync(cancellationToken);
            return SelectWorkflowsAsync(_descriptors, triggerType, evaluate);
        }

        public async Task<IEnumerable<IActivityBlueprint>> GetTriggersAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default)
        {
            _descriptors ??= await GetDescriptorsAsync(cancellationToken);
            return SelectWorkflowsAsync(_descriptors, triggerType, evaluate).Select(result => result.WorkflowBlueprint.Activities.First(x => x.Id == result.ActivityId));
        }

        public async Task UpdateTriggersAsync(IWorkflowBlueprint workflowBlueprint, string? workflowInstanceId, CancellationToken cancellationToken = default)
        {
            if (workflowInstanceId == null)
                _logger.LogDebug("Updating triggers for workflows of {WorkflowDefinitionId}", workflowBlueprint.Id);
            else
                _logger.LogDebug("Updating triggers for workflow {WorkflowInstanceId}", workflowInstanceId);

            _stopwatch.Restart();
            _descriptors ??= await GetDescriptorsAsync(cancellationToken);
            _descriptors[workflowBlueprint.Id] = await BuildDescriptorsForAsync(workflowBlueprint, workflowInstanceId, cancellationToken).ToList();
            _memoryCache.Set(CacheKey, _descriptors);
            _stopwatch.Stop();
            
            if (workflowInstanceId == null)
                _logger.LogDebug("Updated triggers for workflows of {WorkflowDefinitionId} in {ElapsedTime}", workflowBlueprint.Id, _stopwatch.Elapsed);
            else
                _logger.LogDebug("Updated triggers for workflow {WorkflowInstanceId} in {ElapsedTime}", workflowInstanceId, _stopwatch.Elapsed);
        }

        public async Task RemoveTriggerAsync(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            _descriptors ??= await GetDescriptorsAsync(cancellationToken);

            var query =
                from entry in _descriptors
                from descriptor in entry.Value
                where descriptor.Trigger == trigger
                select (entry.Key, descriptor);

            foreach (var (key, descriptor) in query.ToList())
                _descriptors[key].Remove(descriptor);

            _memoryCache.Set(CacheKey, _descriptors);
        }

        private async Task<IDictionary<string, ICollection<TriggerDescriptor>>> GetDescriptorsAsync(CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreateAsync(CacheKey, _ => BuildDescriptorsAsync(cancellationToken));
        }

        private IEnumerable<WorkflowSelectorResult> SelectWorkflowsAsync(
            IDictionary<string, ICollection<TriggerDescriptor>> dictionary,
            Type triggerType,
            Func<ITrigger, bool> evaluate)
        {
            var descriptors = dictionary.SelectMany(x => x.Value);
            return
                from descriptor in descriptors
                let workflow = descriptor.WorkflowBlueprint
                where workflow.IsPublished && workflow.IsEnabled
                let workflowInstanceId = descriptor.WorkflowInstanceId
                let trigger = descriptor.Trigger
                where trigger.GetType() == triggerType && evaluate(trigger)
                select new WorkflowSelectorResult(workflow, workflowInstanceId, descriptor.ActivityId, trigger);
        }

        private async Task<IDictionary<string, ICollection<TriggerDescriptor>>> BuildDescriptorsAsync(CancellationToken cancellationToken)
        {
            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
            var descriptors = new List<TriggerDescriptor>();

            foreach (var workflowBlueprint in workflowBlueprints.Where(x => x.IsEnabled))
            {
                var blueprintDescriptors = await BuildDescriptorsForAsync(workflowBlueprint, null, cancellationToken);
                descriptors.AddRange(blueprintDescriptors);
            }

            return descriptors
                .GroupBy(x => x.WorkflowBlueprint.Id)
                .ToDictionary(x => x.Key, x => (ICollection<TriggerDescriptor>) x.ToList());
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsForAsync(IWorkflowBlueprint workflowBlueprint, string? workflowInstanceId, CancellationToken cancellationToken)
        {
            var descriptors = new List<TriggerDescriptor>();
            
            if (workflowInstanceId != null)
            {    
                descriptors = _descriptors!.ContainsKey(workflowBlueprint.Id) ? _descriptors[workflowBlueprint.Id].ToList() : new List<TriggerDescriptor>();
                descriptors.RemoveAll(x => x.WorkflowInstanceId == workflowInstanceId);
                
                var workflowInstance = await _workflowInstanceStore.FindByIdAsync(workflowInstanceId, cancellationToken);
                var blockingActivities = workflowBlueprint.GetBlockingActivities(workflowInstance!);
                var resumeTriggers = await BuildDescriptorsAsync(workflowBlueprint, blockingActivities, workflowInstance, cancellationToken);
                descriptors.AddRange(resumeTriggers);
                    
                return descriptors;
            }
            
            // Build triggers for workflow blue prints.
            var startTriggers = await BuildDescriptorsAsync(workflowBlueprint, cancellationToken).ToList();
            descriptors.AddRange(startTriggers);

            // Build triggers for workflow instances.
            var specification = new WorkflowInstanceDefinitionIdSpecification(workflowBlueprint.Id)
                    .WithTenant(workflowBlueprint.TenantId)
                    .WithStatus(WorkflowStatus.Suspended);

            var workflowInstances = await _workflowInstanceStore
                .FindManyAsync(specification, cancellationToken: cancellationToken)
                .ToList();

            foreach (var workflowInstance in workflowInstances)
            {
                var blockingActivities = workflowBlueprint.GetBlockingActivities(workflowInstance);
                var resumeTriggers = await BuildDescriptorsAsync(workflowBlueprint, blockingActivities, workflowInstance, cancellationToken);
                descriptors.AddRange(resumeTriggers);
            }

            return descriptors;
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken)
        {
            var startActivities = workflowBlueprint.GetStartActivities();
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);

            // This is a transient workflow instance; setting EntityId to null ensures trigger providers don't try and load the workflow instance. 
            workflowInstance.Id = null!;
            return await BuildDescriptorsAsync(workflowBlueprint, startActivities, workflowInstance, cancellationToken);
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsAsync(
            IWorkflowBlueprint workflowBlueprint,
            IEnumerable<IActivityBlueprint> blockingActivities,
            WorkflowInstance workflowInstance,
            CancellationToken cancellationToken)
        {
            var providers = _triggerProviders.ToList();
            var descriptors = new List<TriggerDescriptor>();
            var scope = _serviceProvider.CreateScope();
            var workflowExecutionContext = new WorkflowExecutionContext(scope, workflowBlueprint, workflowInstance);
            var isTransientWorkflowInstance = workflowInstance.Id == null!;

            workflowExecutionContext.WorkflowContext =
                workflowBlueprint.ContextOptions != null &&
                !isTransientWorkflowInstance &&
                !string.IsNullOrWhiteSpace(workflowInstance.ContextId)
                    ? await _workflowContextManager.LoadContext(new LoadWorkflowContext(workflowExecutionContext), cancellationToken)
                    : default;

            foreach (var blockingActivity in blockingActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, blockingActivity, null, cancellationToken);
                var providerContext = new TriggerProviderContext(activityExecutionContext);

                foreach (var provider in providers)
                {
                    var trigger = await provider.GetTriggerAsync(providerContext, cancellationToken);

                    if (trigger is NullTrigger)
                        continue;

                    var descriptor = new TriggerDescriptor
                    {
                        ActivityId = blockingActivity.Id,
                        ActivityType = blockingActivity.Type,
                        WorkflowBlueprint = workflowBlueprint,
                        WorkflowInstanceId = workflowInstance.Id,
                        Trigger = trigger,
                    };

                    descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }
    }
}