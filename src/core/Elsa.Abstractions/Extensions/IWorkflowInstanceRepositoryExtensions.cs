using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Repositories;

namespace Elsa.Extensions
{
    public static class IWorkflowInstanceRepositoryExtensions
    {
        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(
          this IWorkflowInstanceRepository manager,
          string workflowDefinitionId,
          WorkflowStatus workflowStatus,
          CancellationToken cancellationToken = default) =>
          manager.ListByDefinitionAndStatusAsync(workflowDefinitionId, default, workflowStatus, cancellationToken);

        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
           this IWorkflowInstanceRepository manager,
           string workflowDefinitionId,
           CancellationToken cancellationToken = default) =>
           manager.ListByDefinitionAsync(workflowDefinitionId, default, cancellationToken);
    }
}
