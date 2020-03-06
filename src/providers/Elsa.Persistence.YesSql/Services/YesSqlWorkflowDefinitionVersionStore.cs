using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class YesSqlWorkflowDefinitionVersionStore : IWorkflowDefinitionVersionStore
    {
        private readonly ISession session;
        private readonly IMapper mapper;

        public YesSqlWorkflowDefinitionVersionStore(ISession session, IMapper mapper)
        {
            this.session = session;
            this.mapper = mapper;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definitionVersion,
            CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionVersionDocument>(definitionVersion);
            session.Save(document);
            await session.CommitAsync();
            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionVersionDocument>(definitionVersion);

            session.Save(document);
            await session.CommitAsync();
            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var query = session
                .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersionIndex>()
                .Where(x => x.VersionId == id);

            var document = await query.FirstOrDefaultAsync();

            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string definitionId,
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = session
                .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersionIndex>()
                .Where(x => x.WorkflowDefinitionId == definitionId)
                .WithVersion(version);

            var document = await query.FirstOrDefaultAsync();

            return mapper.Map<WorkflowDefinitionVersion>(document);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = session.Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersionIndex>()
                .WithVersion(version);
            var documents = await query.ListAsync();

            return mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(documents);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(
            WorkflowDefinitionVersion definitionVersion,
            CancellationToken cancellationToken)
        {
            var query = session
                .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersionIndex>()
                .Where(x => x.WorkflowDefinitionId == definitionVersion.DefinitionId)
                .WithVersion(VersionOptions.SpecificVersion(definitionVersion.Version));

            var document = await query.FirstOrDefaultAsync();

            document = mapper.Map(definitionVersion, document);
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

            var definitionVersionDocuments = (await session
                    .Query<WorkflowDefinitionVersionDocument, WorkflowDefinitionVersionIndex>()
                    .Where(x => x.WorkflowDefinitionId == id)
                    .ListAsync())
                .ToList();

            foreach (var document in instanceDocuments)
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