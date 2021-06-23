using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowExecutionLogRecords
{
    public class ActivityTypeSpecification : Specification<WorkflowExecutionLogRecord>
    {
        public string ActivityType { get; set; }
        public ActivityTypeSpecification(string activityType) => ActivityType = activityType;
        public override Expression<Func<WorkflowExecutionLogRecord, bool>> ToExpression() => x => x.ActivityType == ActivityType;
    }
}