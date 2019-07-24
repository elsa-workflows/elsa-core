using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Serialization.Models;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class YesSqlWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ISession session;

        public YesSqlWorkflowInstanceStore(ISession session)
        {
            this.session = session;
        }
        
        public Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            session.Save(instance);
            return Task.CompletedTask;
        }

        public Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return session.Query<WorkflowInstance, WorkflowInstanceIndex>(x => x.WorkflowInstanceId == id).FirstOrDefaultAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken)
        {
            return session.Query<WorkflowInstance, WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == definitionId).ListAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken)
        {
            return session.Query<WorkflowInstance>().ListAsync();
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(string activityType, CancellationToken cancellationToken)
        {
            var workflowInstances = await session.Query<WorkflowInstance, WorkflowInstanceBlockingActivitiesIndex>(x => x.ActivityType == activityType).ListAsync();

            return workflowInstances.GetBlockingActivities();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken)
        {
            return session.Query<WorkflowInstance, WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == definitionId && x.WorkflowStatus == status).ListAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken)
        {
            return session.Query<WorkflowInstance, WorkflowInstanceIndex>(x => x.WorkflowStatus == status).ListAsync();
        }
    }
}