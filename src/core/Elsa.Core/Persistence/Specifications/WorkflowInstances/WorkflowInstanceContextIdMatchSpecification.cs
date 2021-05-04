using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowInstanceContextIdMatchSpecification : Specification<WorkflowInstance>
    {
        public string ContextType { get; set; }
        public string ContextId { get; set; }

        public WorkflowInstanceContextIdMatchSpecification(string contextType, string contextId)
        {
            ContextType = contextType;
            ContextId = contextId;
        }

        public WorkflowInstanceContextIdMatchSpecification(Type contextType, string contextId) : this(contextType.GetContextTypeName(), contextId)
        {
        }

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.ContextType!.Contains(ContextType) && x.ContextId!.Contains(ContextId);

        public override bool IsSatisfiedBy(WorkflowInstance entity) => entity.ContextType != null 
            && entity.ContextType.IndexOf(ContextType, StringComparison.OrdinalIgnoreCase) >= 0
            && entity.ContextId != null
            && entity.ContextId.IndexOf(ContextId, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}