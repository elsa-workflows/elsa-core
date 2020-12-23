using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class TenantSpecification<T> : Specification<T> where T:ITenantScope
    {
        public string? TenantId { get; set; }
        public TenantSpecification(string? tenantId) => TenantId = tenantId;
        public override Expression<Func<T, bool>> ToExpression() => instance => instance.TenantId == TenantId;
    }
}