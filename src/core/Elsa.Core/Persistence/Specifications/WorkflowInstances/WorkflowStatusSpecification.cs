using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowStatusSpecification : Specification<WorkflowInstance>
    {
        public WorkflowStatusSpecification(WorkflowStatus workflowStatus) => WorkflowStatus = workflowStatus;
        public WorkflowStatus WorkflowStatus { get; set; }
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.WorkflowStatus == WorkflowStatus;
    }
}