using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class SuspendedWorkflowInstanceIdSpecification : Specification<SuspendedWorkflowBlockingActivity>
    {
        public string WorkflowInstanceId { get; set; }
        public SuspendedWorkflowInstanceIdSpecification(string workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
        public override Expression<Func<SuspendedWorkflowBlockingActivity, bool>> ToExpression() => x => x.InstanceId == WorkflowInstanceId;
    }
}