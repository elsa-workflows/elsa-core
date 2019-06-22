using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Runtime
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IWorkflowInvoker invoker;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowSerializer workflowSerializer;

        public WorkflowHost(
            IWorkflowInvoker invoker, 
            IWorkflowDefinitionStore workflowDefinitionStore, 
            IWorkflowInstanceStore workflowInstanceStore, 
            IWorkflowSerializer workflowSerializer)
        {
            this.invoker = invoker;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
            this.workflowSerializer = workflowSerializer;
        }

        public async Task TriggerWorkflowsAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            await StartNewWorkflowsAsync(activityName, arguments, cancellationToken);
            await ResumeExistingWorkflowsAsync(activityName, arguments, cancellationToken);
        }

        public async Task<WorkflowExecutionContext> ExecuteWorkflowAsync(Workflow workflow, IActivity startActivity, CancellationToken cancellationToken)
        {
            return await invoker.InvokeAsync(workflow, new[]{startActivity}, cancellationToken);
        }

        private async Task StartNewWorkflowsAsync(string activityType, Variables arguments, CancellationToken cancellationToken)
        {
            var items = await workflowDefinitionStore.ListByStartActivityAsync(activityType, cancellationToken);

            foreach (var item in items)
            {
                //await ExecuteWorkflowAsync(item.Item1, item.Item2, cancellationToken);
            }
        }

        private async Task ResumeExistingWorkflowsAsync(string activityType, Variables arguments, CancellationToken cancellationToken)
        {
            var items = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, cancellationToken);

            foreach (var item in items)
            {
                //await ResumeWorkflowAsync(item.Item1, item.Item2, arguments, cancellationToken);
            }
        }
    }
}