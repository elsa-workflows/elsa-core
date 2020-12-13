using Elsa.Models;

namespace Elsa.Specifications
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T> WithTenant<T>(this ISpecification<T> specification, string? tenantId) where T : ITenantScope => specification.And(new TenantSpecification<T>(tenantId));
        public static ISpecification<WorkflowInstance> WithWorkflowDefinition(this ISpecification<WorkflowInstance> specification, string workflowDefinitionId) => specification.And(new WorkflowDefinitionSpecification(workflowDefinitionId));
    }
}