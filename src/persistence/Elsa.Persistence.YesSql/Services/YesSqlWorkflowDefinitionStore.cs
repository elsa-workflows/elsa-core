using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Extensions;
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

        public Task<WorkflowDefinition> SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionDocument>(definition);
            session.Save(document);
            return Task.FromResult(mapper.Map<WorkflowDefinition>(document));
        }

        public Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionDocument>(definition);

            session.Save(document);
            return Task.CompletedTask;
        }

        public async Task<WorkflowDefinition> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = session
                .Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>()
                .Where(x => x.WorkflowDefinitionId == id)
                .WithVersion(version);

            var document = await query.FirstOrDefaultAsync();

            return mapper.Map<WorkflowDefinition>(document);
        }

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = session.Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>().WithVersion(version);
            var documents = await query.ListAsync();

            return mapper.Map<IEnumerable<WorkflowDefinition>>(documents);
        }

        public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
        {
            var query = session
                .Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>()
                .Where(x => x.WorkflowDefinitionId == definition.Id)
                .WithVersion(VersionOptions.SpecificVersion(definition.Version));
            
            var document = await query.FirstOrDefaultAsync();

            document = mapper.Map(definition, document);
            session.Save(document);

            return mapper.Map<WorkflowDefinition>(document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var documents = (await session
                    .Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>()
                    .Where(x => x.WorkflowDefinitionId == id)
                    .ListAsync())
                .ToList();

            foreach (var document in documents)
            {
                session.Delete(document);
            }

            return documents.Count;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return session.CommitAsync();
        }
    }
}