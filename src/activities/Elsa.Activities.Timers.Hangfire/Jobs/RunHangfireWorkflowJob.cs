using System.Threading.Tasks;
using Elsa.Activities.Timers.Hangfire.Models;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;

namespace Elsa.Activities.Timers.Hangfire.Jobs
{
    public class RunHangfireWorkflowJob
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowQueue _workflowQueue;

        public RunHangfireWorkflowJob(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, IWorkflowQueue workflowQueue)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowQueue = workflowQueue;
        }

        public async Task ExecuteAsync(RunHangfireWorkflowJobModel data)
        {
            var workflowBlueprint = (await _workflowRegistry.GetWorkflowAsync(data.WorkflowDefinitionId, data.TenantId, VersionOptions.Published));
            
            if(workflowBlueprint == null)
                return;

            if (data.WorkflowInstanceId == null)
            {
                if (workflowBlueprint.IsSingleton == false || await GetWorkflowIsAlreadyExecutingAsync(data.TenantId, data.WorkflowDefinitionId) == false) 
                    await _workflowRunner.RunWorkflowAsync(workflowBlueprint, data.ActivityId);
            }
            else
            {
                await _workflowQueue.EnqueueWorkflowInstance(data.WorkflowInstanceId, data.ActivityId, default);
            }
        }
        
        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId)
        {
            var specification = new TenantSpecification<WorkflowInstance>(tenantId).WithWorkflowDefinition(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification());
            return await _workflowInstanceStore.FindAsync(specification) != null;
        }
    }
}
