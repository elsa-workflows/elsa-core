using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowFinishedStatusSpecification : Specification<WorkflowInstance>
    {
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.WorkflowStatus == WorkflowStatus.Cancelled || x.WorkflowStatus == WorkflowStatus.Faulted || x.WorkflowStatus == WorkflowStatus.Finished;
    }
}