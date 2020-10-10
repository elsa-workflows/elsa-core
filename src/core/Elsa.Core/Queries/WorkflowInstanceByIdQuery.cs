using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Persistence;
using YesSql;

namespace Elsa.Queries
{
    public class WorkflowInstanceByIdQuery : ICompiledQuery<WorkflowInstance>
    {
        public string WorkflowInstanceId { get; }

        public WorkflowInstanceByIdQuery(string workflowInstanceId)
        {
            WorkflowInstanceId = workflowInstanceId;
        }

        public Expression<Func<IQuery<WorkflowInstance>, IQuery<WorkflowInstance>>> Query() => query =>
            query.With<WorkflowInstanceIndex>().Where(
                x => x.WorkflowInstanceId == WorkflowInstanceId);
    }

    public static class WorkflowInstanceByIdQuerySessionExtensions
    {
        public static IQuery<WorkflowInstance> QueryWorkflowInstanceById(
            this ISession session,
            string workflowInstanceId) =>
            session
                .ExecuteQuery(
                    new WorkflowInstanceByIdQuery(workflowInstanceId),
                    CollectionNames.WorkflowInstances);

        public static async Task<WorkflowInstance?> GetWorkflowInstanceByIdAsync(
            this ISession session,
            string workflowInstanceId,
            CancellationToken cancellationToken = default) =>
            await session.QueryWorkflowInstanceById(workflowInstanceId).FirstOrDefaultAsync();
    }
}