using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowExecutionLogRecords
{
    public class WorkflowInstanceIdsSpecification : Specification<WorkflowExecutionLogRecord>
    {
        public WorkflowInstanceIdsSpecification(IEnumerable<string> workflowInstanceIds)
        {
            WorkflowInstanceIds = workflowInstanceIds;
        }

        public IEnumerable<string> WorkflowInstanceIds { get; }

        public override Expression<Func<WorkflowExecutionLogRecord, bool>> ToExpression() => x => WorkflowInstanceIds.Contains(x.WorkflowInstanceId);
    }
}