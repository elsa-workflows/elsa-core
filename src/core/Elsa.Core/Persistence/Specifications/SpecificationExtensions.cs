using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T> WithTenant<T>(this ISpecification<T> specification, string? tenantId) where T : ITenantScope => specification.And(new TenantSpecification<T>(tenantId));
    }
}