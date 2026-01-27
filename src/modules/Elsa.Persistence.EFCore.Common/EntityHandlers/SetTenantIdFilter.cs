using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.EntityHandlers;

/// <summary>
/// Represents a class that applies a filter to set the TenantId for entities.
/// </summary>
public class SetTenantIdFilter(ITenantAccessor? tenantAccessor) : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
            return;

        modelBuilder
            .Entity(entityType.ClrType)
            .HasQueryFilter(CreateTenantFilterExpression(entityType.ClrType));
    }

    private LambdaExpression CreateTenantFilterExpression(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");

        // e => EF.Property<string>(e, "TenantId") == tenantAccessor.Tenant.Id
        var tenantIdProperty = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(string)],
            parameter,
            Expression.Constant("TenantId"));

        // Build an expression that accesses tenantAccessor.Tenant.Id
        // Note: While we capture the ITenantAccessor instance here, its Tenant property uses AsyncLocal,
        // which means it will return the correct tenant for the current async execution context at query time.
        // This is safe because each request/scope has its own AsyncLocal value.
        var tenantAccessorConstant = Expression.Constant(tenantAccessor, typeof(ITenantAccessor));
        var tenantProperty = Expression.Property(tenantAccessorConstant, nameof(ITenantAccessor.Tenant));
        var tenantIdOnAccessor = Expression.Property(tenantProperty, nameof(Tenant.Id));

        var body = Expression.Equal(tenantIdProperty, tenantIdOnAccessor);

        return Expression.Lambda(body, parameter);
    }
}