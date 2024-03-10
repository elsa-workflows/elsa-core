using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Elsa.EntityFrameworkCore.Common.EntityHandlers;

/// <summary>
/// 
/// </summary>
public class SetTenantIdFilter : IEntityModelCreatingHandler
{
    /// <inheritdoc />
    public void Handle(ElsaDbContextBase dbContext, ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (entityType.ClrType.IsAssignableTo(typeof(Entity)))
        {
            var parameter = Expression.Parameter(entityType.ClrType);
            Expression<Func<Entity, bool>> filterExpr = entity => dbContext.TenantId == entity.TenantId;
            var body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
            var lambdaExpression = Expression.Lambda(body, parameter);

            entityType.SetQueryFilter(lambdaExpression);
        }
    }
}