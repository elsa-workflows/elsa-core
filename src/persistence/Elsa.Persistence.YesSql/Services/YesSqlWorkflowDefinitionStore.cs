using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class YesSqlWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ISession session;

        public YesSqlWorkflowDefinitionStore(ISession session)
        {
            this.session = session;
        }
        
        public Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            session.Save(definition);
            return Task.CompletedTask;
        }

        public Task<WorkflowDefinition> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return session.Query<WorkflowDefinition, WorkflowDefinitionIndex>(x => x.WorkflowDefinitionId == id).FirstOrDefaultAsync();
        }
    }
}