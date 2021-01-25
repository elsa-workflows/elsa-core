using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;

namespace Elsa
{
    public static class WorkflowInstanceStoreExtensions
    {
        public static Task<WorkflowInstance?> FindByIdAsync(
           this IWorkflowInstanceStore store,
           string id,
           CancellationToken cancellationToken = default) =>
           store.FindAsync(new WorkflowInstanceIdSpecification(id), cancellationToken);
        
        public static Task<WorkflowInstance?> FindByCorrelationIdAsync(
            this IWorkflowInstanceStore store,
            string correlationId,
            CancellationToken cancellationToken = default) =>
            store.FindAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId), cancellationToken);
        
        public static Task<WorkflowInstance?> FindByCorrelationIdAsync(
            this IWorkflowInstanceStore store,
            string correlationId,
            Func<ISpecification<WorkflowInstance>, ISpecification<WorkflowInstance>> specificationBuilder,
            CancellationToken cancellationToken = default) =>
            store.FindAsync( specificationBuilder(new CorrelationIdSpecification<WorkflowInstance>(correlationId)), cancellationToken);
    }
}
