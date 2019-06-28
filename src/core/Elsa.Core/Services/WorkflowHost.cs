using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization.Models;
using Elsa.Services;

namespace Elsa.Core.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IWorkflowRegistry registry;
        private readonly IWorkflowInvoker invoker;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public WorkflowHost(
            IWorkflowRegistry registry,
            IWorkflowInvoker invoker,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.registry = registry;
            this.invoker = invoker;
            this.workflowInstanceStore = workflowInstanceStore;
        }

        public async Task TriggerWorkflowsAsync(string activityType, Variables input, CancellationToken cancellationToken)
        {
            var workflowDefinitions = registry.ListByStartActivity(activityType).ToDictionary(x => x.Item1.Id);
            var workflowInstances = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, cancellationToken).ToListAsync();
            var workflowInstancesByDefinitionId = workflowInstances.ToDictionary(x => x.Item1.DefinitionId);
            var workflowsToStart = workflowDefinitions.Values.Where(x => !workflowInstancesByDefinitionId.ContainsKey(x.Item1.Id));

            await StartWorkflowsAsync(workflowsToStart, input, cancellationToken);
            await ResumeWorkflowsAsync(workflowDefinitions, workflowInstances, input, cancellationToken);
        }

        private async Task StartWorkflowsAsync(IEnumerable<(WorkflowDefinition, ActivityDefinition)> workflowDefinitions, Variables variables, CancellationToken cancellationToken1)
        {
            foreach (var (workflowDefinition, activityDefinition) in workflowDefinitions)
            {
                var startActivities = workflowDefinition.Activities.Where(x => x.Id == activityDefinition.Id).Select(x => x.Id);
                await invoker.InvokeAsync(workflowDefinition, variables, startActivityIds: startActivities, cancellationToken: cancellationToken1);
            }
        }
        
        private async Task ResumeWorkflowsAsync(IReadOnlyDictionary<string, (WorkflowDefinition, ActivityDefinition)> workflowDefinitions, IList<(WorkflowInstance, ActivityInstance)> workflowInstances, Variables input, CancellationToken cancellationToken)
        {
            foreach (var (workflowInstance, startActivityInstance) in workflowInstances)
            {
                var workflowDefinition = workflowDefinitions[workflowInstance.DefinitionId];

                workflowInstance.Status = WorkflowStatus.Resuming;

                await invoker.InvokeAsync(workflowDefinition.Item1, input, workflowInstance, new[] { startActivityInstance.Id }, cancellationToken);
            }
        }
    }
}