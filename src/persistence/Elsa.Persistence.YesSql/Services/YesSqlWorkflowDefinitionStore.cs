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
        private readonly ISessionProvider sessionProvider;
        private readonly IMapper mapper;

        public YesSqlWorkflowDefinitionStore(ISessionProvider sessionProvider, IMapper mapper)
        {
            this.sessionProvider = sessionProvider;
            this.mapper = mapper;
        }
        
        public Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var document = mapper.Map<WorkflowDefinitionDocument>(definition);

            using (var session = sessionProvider.GetSession())
            {
                session.Save(document);
            }

            return Task.CompletedTask;
        }

        public async Task<WorkflowDefinition> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            using (var session = sessionProvider.GetSession())
            {
                var document = await session.Query<WorkflowDefinitionDocument, WorkflowDefinitionIndex>(x => x.WorkflowDefinitionId == id).FirstOrDefaultAsync();

                return mapper.Map<WorkflowDefinition>(document);
            }
        }
    }
}