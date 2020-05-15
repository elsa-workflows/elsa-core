using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Services;
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

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionVersionDocument>(definition);
            session.Save(document);
            await session.CommitAsync();
            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionVersionDocument>(definition);

            session.Save(document);
            await session.CommitAsync();
            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = session
                .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex>()
                .Where(x => x.WorkflowDefinitionId == id)
                .WithVersion(version);

            var document = await query.FirstOrDefaultAsync();

            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = session.Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex>()
                .WithVersion(version);
            var documents = await query.ListAsync();

            return mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(documents);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(
            WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken)
        {
            var query = session
                .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex>()
                .Where(x => x.WorkflowDefinitionId == definition.DefinitionId)
                .WithVersion(VersionOptions.SpecificVersion(definition.Version));

            var document = await query.FirstOrDefaultAsync();

            document = mapper.Map(definition, document);
            session.Save(document);
            await session.CommitAsync();

            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var instanceDocuments = (await session.Query<WorkflowInstanceDocument, WorkflowInstanceIndex>()
                    .Where(x => x.WorkflowDefinitionId == id)
                    .ListAsync())
                .ToList();

            var definitionDocuments = (await session
                    .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionIndex>()
                    .Where(x => x.WorkflowDefinitionId == id)
                    .ListAsync())
                .ToList();

            foreach (var document in instanceDocuments)
            {
                session.Delete(document);
            }

            foreach (var document in definitionDocuments)
            {
                session.Delete(document);
            }

            await session.CommitAsync();
            return definitionDocuments.Count;
        }
    }
}