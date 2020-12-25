using Elsa.Models;
using System;

namespace Elsa.Persistence.Specifications
{
    public static class WorkflowInstancSpecificationExtensions
    {
        public static ISpecification<WorkflowInstance> WithWorkflowDefinition(this ISpecification<WorkflowInstance> specification, string workflowDefinitionId) => specification.And(new WorkflowInstanceDefinitionIdSpecification(workflowDefinitionId));
        public static ISpecification<WorkflowInstance> WithWorkflowName(this ISpecification<WorkflowInstance> specification, string name) => specification.And(new WorkflowInstanceNameMatchSpecification(name));
        public static ISpecification<WorkflowInstance> WithContextId(this ISpecification<WorkflowInstance> specification, string contextType, string contextId) => specification.And(new WorkflowInstanceContextIdMatchSpecification(contextType, contextId));
        public static ISpecification<WorkflowInstance> WithContextId(this ISpecification<WorkflowInstance> specification, Type contextType, string contextId) => specification.And(new WorkflowInstanceContextIdMatchSpecification(contextType, contextId));
        public static ISpecification<WorkflowInstance> WithContextId<TContextType>(this ISpecification<WorkflowInstance> specification, string contextId) => specification.And(new WorkflowInstanceContextIdMatchSpecification(typeof(TContextType), contextId));
        public static ISpecification<WorkflowInstance> WithContext(this ISpecification<WorkflowInstance> specification, string contextType) => specification.And(new WorkflowInstanceContextMatchSpecification(contextType));
        public static ISpecification<WorkflowInstance> WithContext(this ISpecification<WorkflowInstance> specification, Type contextType) => specification.And(new WorkflowInstanceContextMatchSpecification(contextType));
        public static ISpecification<WorkflowInstance> WithContextd<TContextType>(this ISpecification<WorkflowInstance> specification) => specification.And(new WorkflowInstanceContextMatchSpecification(typeof(TContextType)));
        public static ISpecification<WorkflowInstance> WithStatus(this ISpecification<WorkflowInstance> specification, WorkflowStatus status) => specification.And(new WorkflowStatusSpecification(status));

    }
}
