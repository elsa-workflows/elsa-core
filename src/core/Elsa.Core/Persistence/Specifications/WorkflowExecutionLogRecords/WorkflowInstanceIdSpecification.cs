using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowExecutionLogRecords
{
    public class WorkflowInstanceIdSpecification : Specification<WorkflowExecutionLogRecord>
    {
        public string WorkflowInstanceId { get; set; }
        public WorkflowInstanceIdSpecification(string workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
        public override Expression<Func<WorkflowExecutionLogRecord, bool>> ToExpression() => x => x.WorkflowInstanceId == WorkflowInstanceId;
    }
}