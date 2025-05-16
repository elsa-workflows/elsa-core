using System.Linq.Expressions;
using Elsa.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.EntityHandlers;

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

        // e => EF.Property<string>(e, "TenantId") == this.TenantId
        var tenantIdProperty = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(string)],
            parameter,
            Expression.Constant("TenantId"));

        var tenantIdOnContext = Expression.Property(
            Expression.Constant(dbContext),
            nameof(ElsaDbContextBase.TenantId));

        var body = Expression.Equal(tenantIdProperty, tenantIdOnContext);

        return Expression.Lambda(body, parameter);
    }
}