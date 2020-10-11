using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Services;
using YesSql;

namespace Elsa.Queries
{
    public class WorkflowInstanceByBlockingActivityQuery : ICompiledQuery<WorkflowInstance>
    {
        public string ActivityType { get; }

        public WorkflowInstanceByBlockingActivityQuery(string activityType)
        {
            ActivityType = activityType;
        }

        public Expression<Func<IQuery<WorkflowInstance>, IQuery<WorkflowInstance>>> Query() => query =>
            query.With<WorkflowInstanceBlockingActivitiesIndex>().Where(
                x => x.ActivityType == ActivityType);
    }

    public static class WorkflowInstanceByBlockingActivityQueryWorkflowInstanceManagerExtensions
    {
        public static IQuery<WorkflowInstance> QueryByBlockingActivity(
            this IWorkflowInstanceManager manager,
            string activityType) =>
            manager
                .ExecuteQuery(
                    new WorkflowInstanceByBlockingActivityQuery(activityType));

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