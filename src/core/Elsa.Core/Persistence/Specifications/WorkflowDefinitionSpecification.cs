using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowInstanceDefinitionIdSpecification : Specification<WorkflowInstance>
    {
        public string WorkflowDefinitionId { get; set; }
        public WorkflowInstanceDefinitionIdSpecification(string workflowDefinitionId) => WorkflowDefinitionId = workflowDefinitionId;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.DefinitionId == WorkflowDefinitionId;
    }
}