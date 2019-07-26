using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class YesSqlWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ISession session;
        private readonly IMapper mapper;

        public YesSqlWorkflowInstanceStore(ISession session, IMapper mapper)
        {
            this.session = session;
            this.mapper = mapper;
        }

        public Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            var document = mapper.Map<WorkflowInstanceDocument>(instance);

            session.Save(document);
            return Task.CompletedTask;
        }

        public async Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var document = await session.Query<WorkflowInstanceDocument, WorkflowInstanceIndex>(x => x.WorkflowInstanceId == id).FirstOrDefaultAsync();
            return mapper.Map<WorkflowInstance>(document);
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            var document = await session.Query<WorkflowInstanceDocument, WorkflowInstanceIndex>(x => x.CorrelationId == correlationId).FirstOrDefaultAsync();
            return mapper.Map<WorkflowInstance>(document);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken)
        {
            var documents = await session.Query<WorkflowInstanceDocument, WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == definitionId).ListAsync();
            return mapper.Map<IEnumerable<WorkflowInstance>>(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken)
        {
            var documents = await session.Query<WorkflowInstanceDocument>().ListAsync();
            return mapper.Map<IEnumerable<WorkflowInstance>>(documents);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType, 
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = session.Query<WorkflowInstanceDocument, WorkflowInstanceBlockingActivitiesIndex>(x => x.ActivityType == activityType);

            if (string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            var documents = await query.ListAsync();
            var instances = mapper.Map<IEnumerable<WorkflowInstance>>(documents);

            return instances.GetBlockingActivities();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken)
        {
            var documents = await session
                .Query<WorkflowInstanceDocument, WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == definitionId && x.WorkflowStatus == status)
                .ListAsync();
            return mapper.Map<IEnumerable<WorkflowInstance>>(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken)
        {
            var documents = await session.Query<WorkflowInstanceDocument, WorkflowInstanceIndex>(x => x.WorkflowStatus == status).ListAsync();
            return mapper.Map<IEnumerable<WorkflowInstance>>(documents);
        }
    }
}