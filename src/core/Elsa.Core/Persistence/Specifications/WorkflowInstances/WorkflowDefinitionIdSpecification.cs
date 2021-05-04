using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowDefinitionIdSpecification : Specification<WorkflowInstance>
    {
        public string WorkflowDefinitionId { get; set; }
        public WorkflowDefinitionIdSpecification(string workflowDefinitionId) => WorkflowDefinitionId = workflowDefinitionId;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.DefinitionId == WorkflowDefinitionId;
    }
}