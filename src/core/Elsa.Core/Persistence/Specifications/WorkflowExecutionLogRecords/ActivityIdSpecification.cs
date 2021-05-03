using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowExecutionLogRecords
{
    public class ActivityIdSpecification : Specification<WorkflowExecutionLogRecord>
    {
        public string ActivityId { get; set; }
        public ActivityIdSpecification(string activityId) => ActivityId = activityId;
        public override Expression<Func<WorkflowExecutionLogRecord, bool>> ToExpression() => x => x.ActivityId == ActivityId;
    }
}