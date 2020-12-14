using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class CorrelationIdSpecification<T> : Specification<T> where T:ICorrelationScope
    {
        public string? CorrelationId { get; set; }
        public CorrelationIdSpecification(string? correlationId) => CorrelationId = correlationId;
        public override Expression<Func<T, bool>> ToExpression() => x => x.CorrelationId == CorrelationId;
    }

    public class WorkflowInstanceCorrelationIdSpecification : CorrelationIdSpecification<WorkflowInstance>
    {
        public WorkflowInstanceCorrelationIdSpecification(string? correlationId) : base(correlationId)
        {
        }
    }
}