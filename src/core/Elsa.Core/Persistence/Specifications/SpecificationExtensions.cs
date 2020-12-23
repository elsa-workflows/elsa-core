using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T> WithTenant<T>(this ISpecification<T> specification, string? tenantId) where T : ITenantScope => specification.And(new TenantSpecification<T>(tenantId));

        public static ISpecification<WorkflowInstance> WithWorkflowDefinition(this ISpecification<WorkflowInstance> specification, string workflowDefinitionId) =>
            specification.And(new WorkflowInstanceDefinitionIdSpecification(workflowDefinitionId));

        public static ISpecification<WorkflowInstance> WithStatus(this ISpecification<WorkflowInstance> specification, WorkflowStatus status) => specification.And(new WorkflowStatusSpecification(status));
        public static ISpecification<WorkflowDefinition> WithVersionOptions(this ISpecification<WorkflowDefinition> specification, VersionOptions versionOptions) => specification.And(new VersionOptionsSpecification(versionOptions));
    }
}