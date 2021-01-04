using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowDefinitionStore : EntityFrameworkStore<WorkflowDefinition, WorkflowDefinitionEntity>, IWorkflowDefinitionStore
    {
        public EntityFrameworkWorkflowDefinitionStore(ElsaContext dbContext) : base(dbContext)
        {
        }
    }
}