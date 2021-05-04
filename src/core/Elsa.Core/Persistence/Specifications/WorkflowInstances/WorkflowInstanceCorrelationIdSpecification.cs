using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowInstanceCorrelationIdSpecification : Specification<WorkflowInstance>
    {
        public WorkflowInstanceCorrelationIdSpecification(string definitionId, string? correlationId)
        {
            DefinitionId = definitionId;
            CorrelationId = correlationId;
        }
        
        public string DefinitionId { get; }
        public string? CorrelationId { get; }

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.DefinitionId == DefinitionId && x.CorrelationId == CorrelationId;
    }
}