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
        // This will be evaluated at query time, not at model creation time
        var tenantAccessorConstant = Expression.Constant(tenantAccessor, typeof(ITenantAccessor));
        var tenantProperty = Expression.Property(tenantAccessorConstant, nameof(ITenantAccessor.Tenant));
        var tenantIdOnAccessor = Expression.Condition(
            Expression.Equal(tenantProperty, Expression.Constant(null, typeof(Tenant))),
            Expression.Constant(null, typeof(string)),
            Expression.Property(tenantProperty, nameof(Tenant.Id))
        );

        var body = Expression.Equal(tenantIdProperty, tenantIdOnAccessor);

        return Expression.Lambda(body, parameter);
    }
}