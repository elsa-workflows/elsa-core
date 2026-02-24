using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.EFCore.EntityHandlers;

/// <summary>
/// Represents a class that applies a filter to set the TenantId for entities.
/// </summary>
public class SetTenantIdFilter(IOptions<TenantsOptions> tenantsOptions) : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
            return;

        // Only apply the tenant filter if multitenancy is enabled
        if (!tenantsOptions.Value.IsEnabled)
            return;

        modelBuilder
            .Entity(entityType.ClrType)
            .HasQueryFilter(CreateTenantFilterExpression(dbContext, entityType.ClrType));
    }

    private LambdaExpression CreateTenantFilterExpression(ElsaDbContextBase dbContext, Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");

        // e => EF.Property<string>(e, "TenantId") == this.TenantId || EF.Property<string>(e, "TenantId") == "*" || (EF.Property<string>(e, "TenantId") == null && this.TenantId == "")
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

        // For backwards compatibility: include records with null TenantId when context TenantId is empty string
        var nullTenantCheck = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(string)));
        var emptyContextCheck = Expression.Equal(tenantIdOnContext, Expression.Constant(string.Empty, typeof(string)));
        var backwardsCompatibilityCheck = Expression.AndAlso(nullTenantCheck, emptyContextCheck);

        var body = Expression.OrElse(
            Expression.OrElse(equalityCheck, agnosticCheck),
            backwardsCompatibilityCheck);

        return Expression.Lambda(body, parameter);
    }
}