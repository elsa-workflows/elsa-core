using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    /// <summary>
    /// Matches all workflow instances that are idle, running or suspended.
    /// </summary>
    public class UnfinishedWorkflowSpecification : Specification<WorkflowInstance>
    {
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.WorkflowStatus == WorkflowStatus.Idle || x.WorkflowStatus == WorkflowStatus.Running || x.WorkflowStatus == WorkflowStatus.Suspended;
    }
}