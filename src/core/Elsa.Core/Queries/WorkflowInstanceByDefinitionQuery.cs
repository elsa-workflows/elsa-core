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
    public class WorkflowInstanceByDefinitionQuery : ICompiledQuery<WorkflowInstance>
    {
        public string WorkflowDefinitionId { get; }

        public WorkflowInstanceByDefinitionQuery(string workflowDefinitionId) =>
            WorkflowDefinitionId = workflowDefinitionId;

        public Expression<Func<IQuery<WorkflowInstance>, IQuery<WorkflowInstance>>> Query() => query =>
            query.With<WorkflowInstanceIndex>().Where(
                x => x.WorkflowDefinitionId == WorkflowDefinitionId);
    }

    public static class WorkflowInstanceByDefinitionQueryWorkflowInstanceManagerExtensions
    {
        public static Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            this IWorkflowInstanceManager manager,
            string workflowDefinitionId,
            CancellationToken cancellationToken = default) =>
            manager
                .ExecuteQuery(
                    new WorkflowInstanceByDefinitionQuery(workflowDefinitionId))
                .ListAsync();
    }
}