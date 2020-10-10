using System;
using System.Linq.Expressions;
using Elsa.Data;
using Elsa.Indexes;
using Elsa.Models;
using YesSql;

namespace Elsa.Queries
{
    public class WorkflowInstanceByDefinitionAndStatusQuery : ICompiledQuery<WorkflowInstance>
    {
        public string WorkflowDefinitionId { get; }
        public WorkflowStatus Status { get; }

        public WorkflowInstanceByDefinitionAndStatusQuery(string workflowDefinitionId, WorkflowStatus status)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            Status = status;
        }

        public Expression<Func<IQuery<WorkflowInstance>, IQuery<WorkflowInstance>>> Query() => query =>
            query.With<WorkflowInstanceIndex>().Where(
                x => x.WorkflowDefinitionId == WorkflowDefinitionId && x.WorkflowStatus == Status);
    }

    public static class WorkflowInstanceByDefinitionAndStatusQuerySessionExtensions
    {
        public static IQuery<WorkflowInstance> QueryWorkflowInstanceByDefinitionAndStatus(
            this ISession session,
            string workflowDefinitionId,
            WorkflowStatus workflowStatus) =>
            session
                .ExecuteQuery(
                    new WorkflowInstanceByDefinitionAndStatusQuery(workflowDefinitionId, WorkflowStatus.Suspended),
                    CollectionNames.WorkflowInstances);
    }
}