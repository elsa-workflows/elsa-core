using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowTriggers;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Triggers
{
    public class WorkflowTriggerIndexer : IWorkflowTriggerIndexer
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowTriggerStore _workflowTriggerStore;
        private readonly IWorkflowContextManager _workflowContextManager;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly IEnumerable<IWorkflowTriggerProvider> _providers;
        private readonly IIdGenerator _idGenerator;
        private readonly IContentSerializer _contentSerializer;
        private readonly IWorkflowTriggerHasher _hasher;
        private readonly IServiceProvider _serviceProvider;

        public WorkflowTriggerIndexer(
            IWorkflowRegistry workflowRegistry,
            IWorkflowTriggerStore workflowTriggerStore,
            IWorkflowContextManager workflowContextManager,
            IWorkflowFactory workflowFactory,
            IEnumerable<IWorkflowTriggerProvider> providers,
            IIdGenerator idGenerator,
            IContentSerializer contentSerializer,
            IServiceProvider serviceProvider,
            IWorkflowTriggerHasher hasher)
        {
            _workflowRegistry = workflowRegistry;
            _workflowTriggerStore = workflowTriggerStore;
            _workflowContextManager = workflowContextManager;
            _workflowFactory = workflowFactory;
            _providers = providers;
            _idGenerator = idGenerator;
            _contentSerializer = contentSerializer;
            _serviceProvider = serviceProvider;
            _hasher = hasher;
        }

        public async Task IndexTriggersAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            await DeleteTriggersAsync(workflowInstance.Id, cancellationToken);
            
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

            if (workflowBlueprint == null)
                return;

            var blockingActivities = workflowBlueprint.GetBlockingActivities(workflowInstance!);
            var triggerDescriptors = await ExtractTriggersAsync(workflowBlueprint, workflowInstance, blockingActivities, true, cancellationToken);
            await PersistTriggersAsync(triggerDescriptors, workflowInstance, cancellationToken);
        }

        public async Task DeleteTriggersAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdsSpecification(workflowInstanceIds);
            await _workflowTriggerStore.DeleteManyAsync(specification, cancellationToken);
        }

        public async Task DeleteTriggersAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var specification = new WorkflowInstanceIdSpecification(workflowInstanceId);
            await _workflowTriggerStore.DeleteManyAsync(specification, cancellationToken);
        }

        private async Task PersistTriggersAsync(IEnumerable<TriggerDescriptor> triggerDescriptors, WorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            foreach (var triggerDescriptor in triggerDescriptors)
            {
                var records = triggerDescriptor.Triggers.Select(x => new WorkflowTrigger
                {
                    Id = _idGenerator.Generate(),
                    TenantId = workflowInstance.TenantId,
                    ActivityType = triggerDescriptor.ActivityType,
                    ActivityId = triggerDescriptor.ActivityId,
                    WorkflowInstanceId = workflowInstance.Id,
                    Hash = _hasher.Hash(x),
                    Model = _contentSerializer.Serialize(x)
                });

                await _workflowTriggerStore.AddManyAsync(records, cancellationToken);
            }
        }

        private async Task<IEnumerable<TriggerDescriptor>> ExtractTriggersAsync(
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

            // Extract triggers for each blocking activity.
            var triggerDescriptors = new List<TriggerDescriptor>();

            foreach (var blockingActivity in blockingActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(scope, workflowExecutionContext, blockingActivity, null, cancellationToken);
                var providerContext = new TriggerProviderContext(activityExecutionContext);
                var providers = _providers.Where(x => x.ForActivityType == blockingActivity.Type);

                foreach (var provider in providers)
                {
                    var triggers = (await provider.GetTriggersAsync(providerContext, cancellationToken)).ToList();

                    var triggerDescriptor = new TriggerDescriptor
                    {
                        WorkflowBlueprint = workflowBlueprint,
                        WorkflowInstanceId = workflowInstance.Id,
                        ActivityType = blockingActivity.Type,
                        ActivityId = blockingActivity.Id,
                        Triggers = triggers
                    };

                    triggerDescriptors.Add(triggerDescriptor);
                }
            }

            return triggerDescriptors;
        }
    }
}