using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowInstanceStore : EntityFrameworkStore<WorkflowInstance, WorkflowInstanceEntity>, IWorkflowInstanceStore
    {
        public EntityFrameworkWorkflowInstanceStore(ElsaContext dbContext) : base(dbContext)
        {
        }
    }
}