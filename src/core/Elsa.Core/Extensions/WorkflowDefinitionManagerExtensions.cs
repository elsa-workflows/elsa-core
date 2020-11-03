using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Services;
using YesSql;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionManagerExtensions
    {
        public static IQuery<WorkflowDefinition> QueryByIdAndVersion(this IWorkflowDefinitionManager manager, string workflowDefinitionId, VersionOptions versionOptions) =>
            manager.Query<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId).WithVersion(versionOptions);

        public static async Task<WorkflowDefinition?> GetAsync(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByIdAndVersion(workflowDefinitionId, versionOptions).FirstOrDefaultAsync();

        public static Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            this IWorkflowInstanceManager manager,
            WorkflowStatus workflowStatus,
            CancellationToken cancellationToken = default) =>
            manager
                .Query<WorkflowInstanceIndex>(x => x.WorkflowStatus == workflowStatus)
                .ListAsync();
    }
}