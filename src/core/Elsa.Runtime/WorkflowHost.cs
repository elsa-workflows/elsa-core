using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;

namespace Elsa.Runtime
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IWorkflowInvoker invoker;
        private readonly IWorkflowStore workflowStore;
        private readonly IWorkflowSerializer workflowSerializer;

        public WorkflowHost(IWorkflowInvoker invoker, IWorkflowStore workflowStore, IWorkflowSerializer workflowSerializer)
        {
            this.invoker = invoker;
            this.workflowStore = workflowStore;
            this.workflowSerializer = workflowSerializer;
        }

        public async Task TriggerWorkflowAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            await StartNewWorkflowsAsync(activityName, arguments, cancellationToken);
            await ResumeExistingWorkflowsAsync(activityName, arguments, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> StartWorkflowAsync(Workflow workflow, IActivity startActivity, Variables arguments, CancellationToken cancellationToken)
        {
            var workflowInstance = await workflowSerializer.DeriveAsync(workflow, cancellationToken);
            startActivity = workflowInstance.Activities.Single(x => x.Id == startActivity.Id);
            return await invoker.InvokeAsync(workflowInstance, startActivity, arguments, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> ResumeWorkflowAsync(Workflow workflow, IActivity activity, Variables arguments, CancellationToken cancellationToken)
        {
            return await invoker.ResumeAsync(workflow, activity, arguments, cancellationToken);
        }

        private async Task StartNewWorkflowsAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            var workflows = await workflowStore.GetManyAsync(new WorkflowIsDefinition().And(new WorkflowStartsWithActivity(activityName)), cancellationToken);

            foreach (var workflow in workflows)
            {
                var startActivities = workflow.Activities.Where(x => x.Name == activityName && !workflow.Connections.Select(c => c.Target.Activity).Contains(x));

                foreach (var activity in startActivities)
                {
                    await StartWorkflowAsync(workflow, activity, arguments, cancellationToken);
                }
            }
        }

        private async Task ResumeExistingWorkflowsAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            var workflows = await workflowStore.GetManyAsync(new WorkflowIsInstance().And(new WorkflowIsBlockedOnActivity(activityName)), cancellationToken);

            foreach (var workflow in workflows)
            {
                var blockingActivities = workflow.BlockingActivities.Where(x => x.Name == activityName).ToList();

                foreach (var activity in blockingActivities)
                {
                    await ResumeWorkflowAsync(workflow, activity, arguments, cancellationToken);
                }
            }
        }
    }
}