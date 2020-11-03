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

        public static IQuery<WorkflowDefinition> QueryByVersionId(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionVersionId) =>
            manager.Query<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId);

        public static IQuery<WorkflowDefinition> QueryByVersion(
            this IWorkflowDefinitionManager manager,
            VersionOptions versionOptions) =>
            manager.Query<WorkflowDefinitionIndex>().WithVersion(versionOptions);

        public static IQuery<WorkflowInstance> QueryByBlockingActivity(
            this IWorkflowInstanceManager manager,
            string activityType) =>
            manager.Query<WorkflowInstanceBlockingActivitiesIndex>(x => x.ActivityType == activityType);

        public static async Task<WorkflowDefinition?> GetAsync(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByIdAndVersion(workflowDefinitionId, versionOptions).FirstOrDefaultAsync();

        public static async Task<WorkflowDefinition?> GetByVersionIdAsync(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionVersionId,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByVersionId(workflowDefinitionVersionId).FirstOrDefaultAsync();

        public static async Task<IEnumerable<WorkflowDefinition>> ListAsync(
            this IWorkflowDefinitionManager manager,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByVersion(versionOptions).ListAsync();
        
        public static Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            this IWorkflowInstanceManager manager,
            WorkflowStatus workflowStatus,
            CancellationToken cancellationToken = default) =>
            manager
                .Query<WorkflowInstanceIndex>(x => x.WorkflowStatus == workflowStatus)
                .ListAsync();
        
        public static async Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityAsync(
            this IWorkflowInstanceManager manager,
            string activityType,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByBlockingActivity(activityType).ListAsync();

        public static async Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityAsync<TActivity>(
            this IWorkflowInstanceManager manager,
            CancellationToken cancellationToken = default) =>
            await manager.ListByBlockingActivityAsync(typeof(TActivity).Name, cancellationToken);
    }
}