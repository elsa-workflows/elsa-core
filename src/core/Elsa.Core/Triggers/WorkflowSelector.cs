using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NetBox.Extensions;
using Open.Linq.AsyncExtensions;

namespace Elsa.Triggers
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IEnumerable<ITriggerProvider> _triggerProviders;
        private readonly IServiceProvider _serviceProvider;
        private ICollection<TriggerDescriptor>? _descriptors;

        public WorkflowSelector(IWorkflowRegistry workflowRegistry, IWorkflowFactory workflowFactory, IEnumerable<ITriggerProvider> triggerProviders, IServiceProvider serviceProvider)
        {
            _workflowRegistry = workflowRegistry;
            _workflowFactory = workflowFactory;
            _triggerProviders = triggerProviders;
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default)
        {
            _descriptors ??= await BuildDescriptorsAsync(cancellationToken).ToList();
            return SelectWorkflowsAsync(_descriptors, triggerType, evaluate);
        }

        public async Task<IEnumerable<IActivityBlueprint>> GetTriggersAsync(
            IWorkflowBlueprint workflowBlueprint,
            IEnumerable<IActivityBlueprint> blockingActivities,
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default)
        {
            _descriptors ??= await BuildDescriptorsAsync(workflowBlueprint, blockingActivities, cancellationToken).ToList();
            return SelectWorkflowsAsync(_descriptors, triggerType, evaluate).Select(result => result.WorkflowBlueprint.Activities.First(x => x.Id == result.ActivityId));
        }

        private IEnumerable<WorkflowSelectorResult> SelectWorkflowsAsync(
            IEnumerable<TriggerDescriptor> descriptors,
            Type triggerType,
            Func<ITrigger, bool> evaluate) =>
            from descriptor in descriptors
            let trigger = descriptor.Trigger
            where trigger.GetType() == triggerType && evaluate(trigger)
            select new WorkflowSelectorResult(descriptor.WorkflowBlueprint, descriptor.ActivityId);

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsAsync(CancellationToken cancellationToken)
        {
            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
            var descriptors = new List<TriggerDescriptor>();

            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var workflowTriggerDescriptors = await BuildDescriptorsAsync(workflowBlueprint, cancellationToken);
                descriptors.AddRange(workflowTriggerDescriptors);
            }

            return descriptors;
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken)
        {
            var startActivities = workflowBlueprint.GetStartActivities();
            return await BuildDescriptorsAsync(workflowBlueprint, startActivities, cancellationToken);
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsAsync(IWorkflowBlueprint workflowBlueprint, IEnumerable<IActivityBlueprint> blockingActivities, CancellationToken cancellationToken)
        {
            var providers = _triggerProviders.ToList();
            var descriptors = new List<TriggerDescriptor>();
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            var workflowExecutionContext = new WorkflowExecutionContext(_serviceProvider, workflowBlueprint, default!, workflowInstance);

            foreach (var blockingActivity in blockingActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, _serviceProvider, blockingActivity);
                var providerContext = new TriggerProviderContext(activityExecutionContext);

                foreach (var provider in providers)
                {
                    var trigger = await provider.GetTriggerAsync(providerContext, cancellationToken);
                    var descriptor = new TriggerDescriptor
                    {
                        ActivityId = blockingActivity.Id,
                        ActivityType = blockingActivity.Type,
                        WorkflowBlueprint = workflowBlueprint,
                        Trigger = trigger,
                    };

                    descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }
    }
}