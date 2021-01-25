using System;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class BlockingActivityTypeSpecification : Specification<WorkflowInstance>
    {
        public string ActivityType { get; }
        public BlockingActivityTypeSpecification(string activityType) => ActivityType = activityType;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => instance => instance.BlockingActivities.Any(x => x.ActivityType == ActivityType);
    }
}