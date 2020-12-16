using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class BlockingActivityTypeSpecification : Specification<SuspendedWorkflowBlockingActivity>
    {
        public string ActivityType { get; }
        public BlockingActivityTypeSpecification(string activityType) => ActivityType = activityType;
        public override Expression<Func<SuspendedWorkflowBlockingActivity, bool>> ToExpression() => instance => instance.ActivityType == ActivityType;
    }
}