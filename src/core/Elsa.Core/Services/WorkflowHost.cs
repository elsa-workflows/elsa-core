using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
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

        public async Task TriggerWorkflowsAsync(string activityName, Variables input, CancellationToken cancellationToken)
        {
            await StartNewWorkflowsAsync(activityName, input, cancellationToken);
            await ResumeExistingWorkflowsAsync(activityName, input, cancellationToken);
        }

        private async Task StartNewWorkflowsAsync(string activityType, Variables input, CancellationToken cancellationToken)
        {
            var items = registry.ListByStartActivity(activityType);

            foreach (var (workflowDefinition, activityDefinition) in items)
            {
                var startActivities = workflowDefinition.Activities.Where(x => x.Id == activityDefinition.Id).Select(x => x.Id);
                await invoker.InvokeAsync(workflowDefinition, input, startActivityIds: startActivities, cancellationToken: cancellationToken);
            }
        }

        private async Task ResumeExistingWorkflowsAsync(string activityType, Variables input, CancellationToken cancellationToken)
        {
            var items = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, cancellationToken).ToListAsync();

            foreach (var (workflowInstance, startActivityInstance) in items)
            {
                var workflowDefinition = registry.GetById(workflowInstance.DefinitionId);
                var startActivityIds = workflowDefinition.Activities.Where(x => x.Id == startActivityInstance.Id).Select(x => x.Id);

                workflowInstance.Status = WorkflowStatus.Resuming;

                await invoker.InvokeAsync(workflowDefinition, input, workflowInstance, startActivityIds, cancellationToken);
            }
        }
    }
}