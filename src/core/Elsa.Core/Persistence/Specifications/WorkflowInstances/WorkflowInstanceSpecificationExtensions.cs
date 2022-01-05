using System;
using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public static class WorkflowInstanceSpecificationExtensions
    {
        public static ISpecification<WorkflowInstance> WithWorkflowDefinition(this ISpecification<WorkflowInstance> specification, string workflowDefinitionId) =>
            specification.And(new WorkflowDefinitionIdSpecification(workflowDefinitionId));

        public static ISpecification<WorkflowInstance> WithWorkflowDefinitionVersionIds(this ISpecification<WorkflowInstance> specification, IEnumerable<string> workflowDefinitionVersionIds) =>
            specification.And(new WorkflowDefinitionVersionIdsSpecification(workflowDefinitionVersionIds));

        public static ISpecification<WorkflowInstance> WithWorkflowName(this ISpecification<WorkflowInstance> specification, string name) => specification.And(new WorkflowInstanceNameMatchSpecification(name));
        public static ISpecification<WorkflowInstance> WithCorrelationId(this ISpecification<WorkflowInstance> specification, string correlationId) => specification.And(new CorrelationIdSpecification<WorkflowInstance>(correlationId));

        public static ISpecification<WorkflowInstance> WithContextId(this ISpecification<WorkflowInstance> specification, string contextType, string contextId) =>
            specification.And(new WorkflowInstanceContextIdMatchSpecification(contextType, contextId));

        public static ISpecification<WorkflowInstance> WithContextId(this ISpecification<WorkflowInstance> specification, Type contextType, string contextId) =>
            specification.And(new WorkflowInstanceContextIdMatchSpecification(contextType, contextId));

        public static ISpecification<WorkflowInstance> WithContextId<TContextType>(this ISpecification<WorkflowInstance> specification, string contextId) =>
            specification.And(new WorkflowInstanceContextIdMatchSpecification(typeof(TContextType), contextId));

        public static ISpecification<WorkflowInstance> WithContext(this ISpecification<WorkflowInstance> specification, string contextType) => specification.And(new WorkflowInstanceContextMatchSpecification(contextType));
        public static ISpecification<WorkflowInstance> WithContext(this ISpecification<WorkflowInstance> specification, Type contextType) => specification.And(new WorkflowInstanceContextMatchSpecification(contextType));
        public static ISpecification<WorkflowInstance> WithContextId<TContextType>(this ISpecification<WorkflowInstance> specification) => specification.And(new WorkflowInstanceContextMatchSpecification(typeof(TContextType)));
        public static ISpecification<WorkflowInstance> WithStatus(this ISpecification<WorkflowInstance> specification, WorkflowStatus status) => specification.And(new WorkflowStatusSpecification(status));
        public static ISpecification<WorkflowInstance> WithSearchTerm(this ISpecification<WorkflowInstance> specification, string searchTerm) => specification.And(new WorkflowSearchTermSpecification(searchTerm));
    }
}