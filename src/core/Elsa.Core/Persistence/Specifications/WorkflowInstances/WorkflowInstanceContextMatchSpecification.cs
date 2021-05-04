using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowInstanceContextMatchSpecification : Specification<WorkflowInstance>
    {
        public string ContextType { get; set; }

        public WorkflowInstanceContextMatchSpecification(string contextType)
        {
            ContextType = contextType;
        }

        public WorkflowInstanceContextMatchSpecification(Type contextType) : this(contextType.GetContextTypeName())
        {
        }

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.ContextType!.Contains(ContextType);

        public override bool IsSatisfiedBy(WorkflowInstance entity) => entity.ContextType != null
            && entity.ContextType.IndexOf(ContextType, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}