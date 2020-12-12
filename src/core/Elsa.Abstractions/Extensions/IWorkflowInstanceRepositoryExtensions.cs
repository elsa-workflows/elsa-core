using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Extensions
{
    public static class IWorkflowInstanceRepositoryExtensions
    {
        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(
          this IWorkflowInstanceStore manager,
          string workflowDefinitionId,
          WorkflowStatus workflowStatus,
          CancellationToken cancellationToken = default) =>
          manager.ListByDefinitionAndStatusAsync(workflowDefinitionId, default, workflowStatus, cancellationToken);

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
           this IWorkflowInstanceStore manager,
           string workflowDefinitionId,
           CancellationToken cancellationToken = default) =>
           manager.ListByDefinitionAsync(workflowDefinitionId, default, cancellationToken);
    }
}
