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
    public class WorkflowInstanceByStatusQuery : ICompiledQuery<WorkflowInstance>
    {
        public WorkflowStatus Status { get; }

        public WorkflowInstanceByStatusQuery(WorkflowStatus status)
        {
            Status = status;
        }

        public Expression<Func<IQuery<WorkflowInstance>, IQuery<WorkflowInstance>>> Query() => query =>
            query.With<WorkflowInstanceIndex>().Where(
                x => x.WorkflowStatus == Status);
    }

    public static class WorkflowInstanceByStatusQueryWorkflowInstanceManagerExtensions
    {
        public static Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            this IWorkflowInstanceManager manager,
            WorkflowStatus workflowStatus,
            CancellationToken cancellationToken = default) =>
            manager
                .ExecuteQuery(
                    new WorkflowInstanceByStatusQuery(workflowStatus))
                .ListAsync();
    }
}