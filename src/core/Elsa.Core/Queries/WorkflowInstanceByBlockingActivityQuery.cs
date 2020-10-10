using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Persistence;
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

    public static class WorkflowInstanceByBlockingActivityQuerySessionExtensions
    {
        public static IQuery<WorkflowInstance> QueryWorkflowInstanceByBlockingActivity(
            this ISession session,
            string activityType) =>
            session
                .ExecuteQuery(
                    new WorkflowInstanceByBlockingActivityQuery(activityType),
                    CollectionNames.WorkflowInstances);

        public static async Task<IEnumerable<WorkflowInstance>> ListWorkflowInstancesByBlockingActivityAsync(
            this ISession session,
            string activityType,
            CancellationToken cancellationToken) =>
            await session.QueryWorkflowInstanceByBlockingActivity(activityType).ListAsync();

        public static async Task<IEnumerable<WorkflowInstance>> ListWorkflowInstancesByBlockingActivityAsync<TActivity>(
            this ISession session,
            CancellationToken cancellationToken) =>
            await session.QueryWorkflowInstanceByBlockingActivity(typeof(TActivity).Name).ListAsync();
    }
}