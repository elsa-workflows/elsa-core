using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Data;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Services;
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

    public static class WorkflowInstanceByIdQueryWorkflowInstanceManagerExtensions
    {
        public static async Task<WorkflowInstance?> GetByWorkflowInstanceIdAsync(
            this IWorkflowInstanceManager manager,
            string workflowInstanceId,
            CancellationToken cancellationToken = default) =>
            await manager.ExecuteQuery(new WorkflowInstanceByIdQuery(workflowInstanceId)).FirstOrDefaultAsync();
    }
}