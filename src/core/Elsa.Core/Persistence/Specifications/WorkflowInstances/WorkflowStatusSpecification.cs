using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowStatusSpecification : Specification<WorkflowInstance>
    {
        public WorkflowStatus WorkflowStatus { get; set; }
        public WorkflowStatusSpecification(WorkflowStatus workflowStatus) => WorkflowStatus = workflowStatus;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.WorkflowStatus == WorkflowStatus;
    }
}