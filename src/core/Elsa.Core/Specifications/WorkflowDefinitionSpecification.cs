using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Specifications
{
    public class WorkflowDefinitionSpecification : Specification<WorkflowInstance>
    {
        public string WorkflowDefinitionId { get; set; }
        public WorkflowDefinitionSpecification(string workflowDefinitionId) => WorkflowDefinitionId = workflowDefinitionId;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.WorkflowDefinitionId == WorkflowDefinitionId;
    }
}