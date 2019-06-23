using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Runtime
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IWorkflowRegistry registry;
        private readonly IWorkflowFactory workflowFactory;
        private readonly IWorkflowInvoker invoker;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IWorkflowSerializer workflowSerializer;

        public WorkflowHost(
            IWorkflowRegistry registry,
            IWorkflowFactory workflowFactory,
            IWorkflowInvoker invoker,
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowSerializer workflowSerializer)
        {
            this.registry = registry;
            this.workflowFactory = workflowFactory;
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
            return await invoker.InvokeAsync(workflow, new[] { startActivity }, cancellationToken);
        }

        private async Task StartNewWorkflowsAsync(string activityType, Variables input, CancellationToken cancellationToken)
        {
            var items = registry.ListByStartActivity(activityType, cancellationToken);

            foreach (var (workflowBlueprint, activityBlueprint) in items)
            {
                var workflow = workflowFactory.CreateWorkflow(workflowBlueprint, input);
                var startActivity = workflow.Activities.First(x => x.Id == activityBlueprint.Id);
                await ExecuteWorkflowAsync(workflow, startActivity, cancellationToken);
            }
        }

        private async Task ResumeExistingWorkflowsAsync(string activityType, Variables input, CancellationToken cancellationToken)
        {
            var items = await workflowInstanceStore.ListByBlockingActivityAsync(activityType, cancellationToken);

            foreach (var (workflowInstance, startActivityInstance) in items)
            {
                var workflowBlueprint = registry.GetById(workflowInstance.DefinitionId, cancellationToken);
                var workflow = workflowFactory.CreateWorkflow(workflowBlueprint, input, workflowInstance);
                var startActivity = workflow.Activities.First(x => x.Id == startActivityInstance.Id);

                workflow.Status = WorkflowStatus.Resuming;

                await invoker.InvokeAsync(workflow, new[] { startActivity }, cancellationToken);
            }
        }
    }
}