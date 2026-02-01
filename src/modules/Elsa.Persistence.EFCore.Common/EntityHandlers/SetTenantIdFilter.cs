using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore.EntityHandlers;

/// <summary>
/// Represents a class that applies a filter to set the TenantId for entities.
/// </summary>
public class SetTenantIdFilter : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
            return;

        modelBuilder
            .Entity(entityType.ClrType)
            .HasQueryFilter(CreateTenantFilterExpression(dbContext, entityType.ClrType));
    }

    private LambdaExpression CreateTenantFilterExpression(ElsaDbContextBase dbContext, Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");

        // e => EF.Property<string>(e, "TenantId") == this.TenantId || EF.Property<string>(e, "TenantId") == "*"
        var tenantIdProperty = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(string)],
            parameter,
            Expression.Constant("TenantId"));

        var tenantIdOnContext = Expression.Property(
            Expression.Constant(dbContext),
            nameof(ElsaDbContextBase.TenantId));

        var equalityCheck = Expression.Equal(tenantIdProperty, tenantIdOnContext);
        var agnosticCheck = Expression.Equal(tenantIdProperty, Expression.Constant(Tenant.AgnosticTenantId, typeof(string)));
        var body = Expression.OrElse(equalityCheck, agnosticCheck);

        return Expression.Lambda(body, parameter);
    }
}