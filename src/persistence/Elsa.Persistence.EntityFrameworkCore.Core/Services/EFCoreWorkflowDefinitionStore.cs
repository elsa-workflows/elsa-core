using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.EntityFrameworkCore.Core.Services
{
    public class EFCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        public Task SaveAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> DeleteManyAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(ISpecification<WorkflowDefinition> specification, IOrderBy<WorkflowDefinition>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> CountAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<WorkflowDefinition?> FindAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}