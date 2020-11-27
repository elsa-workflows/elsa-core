using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Caching.Memory;
using Open.Linq.AsyncExtensions;

namespace Elsa.Triggers
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private const string CacheKey = "WorkflowSelector:Triggers";
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IWorkflowBlueprintReflector _workflowBlueprintReflector;
        private readonly IEnumerable<ITriggerProvider> _triggerProviders;
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceProvider _serviceProvider;
        private IDictionary<string, ICollection<TriggerDescriptor>>? _descriptors;

        public WorkflowSelector(
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
            IWorkflowInstanceManager workflowInstanceManager,
            IWorkflowContextManager workflowContextManager,
            IWorkflowBlueprintReflector workflowBlueprintReflector,
            IEnumerable<ITriggerProvider> triggerProviders,
            IMemoryCache memoryCache,
            IServiceProvider serviceProvider)
        {
            _workflowRegistry = workflowRegistry;
            _workflowFactory = workflowFactory;
            _workflowInstanceManager = workflowInstanceManager;
            _workflowContextManager = workflowContextManager;
            _workflowBlueprintReflector = workflowBlueprintReflector;
            _triggerProviders = triggerProviders;
            _memoryCache = memoryCache;
            _serviceProvider = serviceProvider;
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

        public async Task UpdateTriggersAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            _descriptors ??= await GetDescriptorsAsync(cancellationToken);
            _descriptors[workflowBlueprint.Id] = await BuildDescriptorsForAsync(workflowBlueprint, cancellationToken).ToList();
            _memoryCache.Set(CacheKey, _descriptors);
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

            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var blueprintDescriptors = await BuildDescriptorsForAsync(workflowBlueprint, cancellationToken);
                descriptors.AddRange(blueprintDescriptors);
            }

            return descriptors
                .GroupBy(x => x.WorkflowBlueprint.Id)
                .ToDictionary(x => x.Key, x => (ICollection<TriggerDescriptor>) x.ToList());
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsForAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken)
        {
            var descriptors = new List<TriggerDescriptor>();

            // Build triggers for workflow blue prints.
            var startTriggers = await BuildDescriptorsAsync(workflowBlueprint, cancellationToken).ToList();
            descriptors.AddRange(startTriggers);

            // Build triggers for workflow instances. 
            var workflowInstances = await _workflowInstanceManager.ListByDefinitionAndStatusAsync(workflowBlueprint.Id, workflowBlueprint.TenantId, WorkflowStatus.Suspended, cancellationToken).ToList();

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
            var workflowExecutionContext = new WorkflowExecutionContext(_serviceProvider, workflowBlueprint, workflowInstance, default);
            workflowExecutionContext.WorkflowContext = workflowBlueprint.ContextOptions != null ? await _workflowContextManager.LoadContext(new LoadWorkflowContext(workflowExecutionContext), cancellationToken) : default;

            foreach (var blockingActivity in blockingActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, _serviceProvider, blockingActivity);
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
                        WorkflowInstanceId = workflowInstance.Id == 0 ? default : workflowInstance.WorkflowInstanceId,
                        Trigger = trigger,
                    };

                    descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }
    }
}