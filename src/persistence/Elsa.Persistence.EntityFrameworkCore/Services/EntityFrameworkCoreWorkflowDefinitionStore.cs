using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Documents;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ElsaContext dbContext;
        private readonly IMapper mapper;

        public EntityFrameworkCoreWorkflowDefinitionStore(ElsaContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken = default)
        {
            var document = Map(definition);

            await dbContext.WorkflowDefinitionVersions.Upsert(document)
                .On(x => new { x.Id })
                .RunAsync(cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            return definition;
        }

        public async Task AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            await dbContext.WorkflowDefinitionVersions.AddAsync(document, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitionVersions
                .AsQueryable()
                .Where(x => x.DefinitionId == id)
                .WithVersion(version);

            var document = await query.FirstOrDefaultAsync(cancellationToken);
            return Map(document);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitionVersions.AsQueryable().WithVersion(version);
            var documents = await query.ToListAsync(cancellationToken);

            return mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(documents);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(
            WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken)
        {
            var document = await dbContext.WorkflowDefinitionVersions.FindAsync(definition.Id);

            document = mapper.Map(definition, document);
            dbContext.WorkflowDefinitionVersions.Update(document);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var definitionRecords = await dbContext.WorkflowDefinitionVersions
                .Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var instanceRecords = await dbContext
                .WorkflowInstances.Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            dbContext.WorkflowInstances.RemoveRange(instanceRecords);
            dbContext.WorkflowDefinitionVersions.RemoveRange(definitionRecords);

            await dbContext.SaveChangesAsync(cancellationToken);

            return definitionRecords.Count;
        }

        private WorkflowDefinitionVersionDocument Map(WorkflowDefinitionVersion source)
        {
            return mapper.Map<WorkflowDefinitionVersionDocument>(source);
        }

        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionDocument source)
        {
            return mapper.Map<WorkflowDefinitionVersion>(source);
        }
    }
}