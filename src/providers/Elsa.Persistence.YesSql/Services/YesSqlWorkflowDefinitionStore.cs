using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class YesSqlWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ISession session;
        private readonly IMapper mapper;
        public YesSqlWorkflowDefinitionStore(ISession session, IMapper mapper)
        {
            this.session = session;
            this.mapper = mapper;
        }
        public async Task<WorkflowDefinition> SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionDocument>(definition);
            session.Save(document);
            await session.CommitAsync();
            return mapper.Map<WorkflowDefinition>(document);
        }
        public async Task<WorkflowDefinition> AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionDocument>(definition);

            session.Save(document);
            await session.CommitAsync();
            return mapper.Map<WorkflowDefinition>(document);
        }

        public async Task<WorkflowDefinition> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var query = session
                .Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>()
                .Where(x => x.Id == id);

            var document = await query.FirstOrDefaultAsync();

            return mapper.Map<WorkflowDefinition>(document);
        }

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(string tenantId = "", CancellationToken cancellationToken = default)
        {
            var query = tenantId != "" ?
                session.Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>().Where(x => x.TenantId == tenantId) :
                session.Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>();

            var documents = await query.ListAsync();

            return mapper.Map<IEnumerable<WorkflowDefinition>>(documents);
        }
        public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var query = session
                .Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>()
                .Where(x => x.Id == definition.Id);

            var document = await query.FirstOrDefaultAsync();

            document = mapper.Map(definition, document);
            session.Save(document);
            await session.CommitAsync();

            return mapper.Map<WorkflowDefinition>(document);
        }
        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var definitionDocuments = (await session.Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>()
                    .Where(x => x.Id == id)
                    .ListAsync())
                .ToList();

            var definitionVersionDocuments = (await session
                    .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersionIndex>()
                    .Where(x => x.WorkflowDefinitionId == id)
                    .ListAsync())
                .ToList();

            foreach (var document in definitionDocuments)
            {
                session.Delete(document);
            }

            foreach (var document in definitionVersionDocuments)
            {
                session.Delete(document);
            }

            await session.CommitAsync();
            return definitionVersionDocuments.Count;
        }
    }
}
