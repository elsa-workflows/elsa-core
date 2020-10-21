using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
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

            var query =
                from descriptor in _descriptors
                let trigger = descriptor.Trigger
                where trigger.GetType() == triggerType && evaluate(trigger)
                select new WorkflowSelectorResult(descriptor.WorkflowBlueprint, descriptor.ActivityId);

            return query;
        }

        private async Task<IEnumerable<TriggerDescriptor>> BuildDescriptorsAsync(CancellationToken cancellationToken)
        {
            var providers = _triggerProviders.ToList();
            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
            var descriptors = new List<TriggerDescriptor>();

            using var scope = _serviceProvider.CreateScope();

            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
                var workflowExecutionContext = new WorkflowExecutionContext(scope.ServiceProvider, workflowBlueprint, workflowInstance);
                var startActivities = workflowBlueprint.GetStartActivities();

                foreach (var startActivity in startActivities)
                {
                    var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, scope.ServiceProvider, startActivity);
                    var providerContext = new TriggerProviderContext(activityExecutionContext);

                    foreach (var provider in providers)
                    {
                        var trigger = await provider.GetTriggerAsync(providerContext, cancellationToken);
                        var descriptor = new TriggerDescriptor
                        {
                            ActivityId = startActivity.Id,
                            ActivityType = startActivity.Type,
                            WorkflowBlueprint = workflowBlueprint,
                            Trigger = trigger,
                        };

                        descriptors.Add(descriptor);
                    }
                }
            }

            return descriptors;
        }
    }
}