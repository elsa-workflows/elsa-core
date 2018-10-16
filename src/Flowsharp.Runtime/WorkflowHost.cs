using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Models;
using Flowsharp.Persistence.Specifications;
using Flowsharp.Runtime.Abstractions;

namespace Flowsharp.Runtime
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IWorkflowInvoker invoker;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public WorkflowHost(IWorkflowInvoker invoker, IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowInstanceStore workflowInstanceStore)
        {
            this.invoker = invoker;
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public async Task TriggerWorkflowAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            await StartNewWorkflowsAsync(activityName, arguments, cancellationToken);
            await ResumeExistingWorkflowsAsync(activityName, arguments, cancellationToken);
        }

        private async Task StartNewWorkflowsAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            var workflowDefinitions = await workflowDefinitionStore.GetManyAsync(new WorkflowStartsWithActivity(activityName), cancellationToken);

            foreach (var workflowDefinition in workflowDefinitions)
            {
                var startActivities = workflowDefinition.Workflow.Activities.Where(x => x.Name == activityName && !workflowDefinition.Workflow.Connections.Select(c => c.Target.Activity).Contains(x));

                foreach (var activity in startActivities)
                {
                    var workflowContext = await invoker.InvokeAsync(workflowDefinition.Workflow, activity, arguments, cancellationToken);
                    var workflowInstance = new WorkflowInstance
                    {
                        WorkflowDefinitionId = workflowDefinition.Id,
                        Id = Guid.NewGuid().ToString(),
                        Workflow = workflowContext.Workflow
                    };

                    await workflowInstanceStore.AddAsync(workflowInstance, cancellationToken);
                }
            }
        }
        
        private async Task ResumeExistingWorkflowsAsync(string activityName, Variables arguments, CancellationToken cancellationToken)
        {
            var workflowInstances = await workflowInstanceStore.GetManyAsync(new WorkflowIsBlockedOnActivity(activityName), cancellationToken);

            foreach (var workflowInstance in workflowInstances)
            {
                var blockingActivities = workflowInstance.Workflow.BlockingActivities.Where(x => x.Name == activityName).ToList();
                
                foreach (var activity in blockingActivities)
                {
                    await invoker.ResumeAsync(workflowInstance.Workflow, activity, arguments, cancellationToken);
                    await workflowInstanceStore.UpdateAsync(workflowInstance, cancellationToken);
                }
            }
        }
    }
}