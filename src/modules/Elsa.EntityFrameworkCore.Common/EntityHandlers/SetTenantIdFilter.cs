using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Elsa.EntityFrameworkCore.EntityHandlers;

/// <summary>
/// Represents a class that applies a filter to set the TenantId for entities.
/// </summary>
public class SetTenantIdFilter : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!entityType.ClrType.IsAssignableTo(typeof(Entity)))
            return;

        var tenantId = dbContext.TenantId.NullIfEmpty();
        var parameter = Expression.Parameter(entityType.ClrType);
        Expression<Func<Entity, bool>> filterExpr = entity => entity.TenantId == tenantId;
        var body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
        var lambdaExpression = Expression.Lambda(body, parameter);

        entityType.SetQueryFilter(lambdaExpression);
    }
}