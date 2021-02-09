using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowInstanceIdsSpecification : Specification<WorkflowInstance>
    {
        public WorkflowInstanceIdsSpecification(IEnumerable<string> workflowInstanceIds)
        {
            WorkflowInstanceIds = workflowInstanceIds;
        }

        public IEnumerable<string> WorkflowInstanceIds { get; }

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => workflowInstance => WorkflowInstanceIds.Contains(workflowInstance.Id);
    }
}