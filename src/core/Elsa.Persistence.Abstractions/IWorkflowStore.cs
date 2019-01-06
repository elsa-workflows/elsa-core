using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence
{
    public interface IWorkflowStore
    {
        Task<IEnumerable<Workflow>> GetManyAsync(ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken);
        Task<Workflow> GetAsync(ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken);
        Task AddAsync(Workflow value, CancellationToken cancellationToken);
        Task UpdateAsync(Workflow value, CancellationToken cancellationToken);
        Task SaveAsync(Workflow value, CancellationToken cancellationToken);
    }

    public static class WorkflowStoreExtensions
    {
        public static Task<Workflow> GetAsync(this IWorkflowStore workflowStore, string id, CancellationToken cancellationToken)
        {
            return workflowStore.GetAsync(new WorkflowById(id), cancellationToken);
        }
        
        public static Task<IEnumerable<Workflow>> GetAllAsync(this IWorkflowStore workflowStore, CancellationToken cancellationToken)
        {
            return workflowStore.GetManyAsync(new AllWorkflows(), cancellationToken);
        }
    }
}