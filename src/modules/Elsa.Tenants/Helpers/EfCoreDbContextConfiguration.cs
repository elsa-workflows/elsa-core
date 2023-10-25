using Elsa.Common.Entities;
using Elsa.Tenants.Accessors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace Elsa.Tenants.Helpers;
public static class EfCoreDbContextConfiguration
{
    /// <summary>
    /// Build a configuration that will be add on EfCore DbContext to filter all the data by the current
    /// </summary>
    /// <returns>Configuration for the DbContext</returns>
    public static Action<ModelBuilder, IServiceProvider> BuildTenantFilterConfiguration()
    {
        return async (modelBuilder, serviceProvider) =>
        {
            //Add global filter on DbContext to split data between tenants
            ITenantAccessor tenantAccessor = serviceProvider.GetRequiredService<ITenantAccessor>();
            var tenant = await tenantAccessor.GetCurrentTenantAsync();

            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                //IEnumerable<IModelCreatingDbContextStrategy> modelCreatingDbContextStrategies = _dbContextStrategies
                //    .OfType<IModelCreatingDbContextStrategy>()
                //    .Where(strategy => strategy.CanExecute(modelBuilder, entityType));

                //foreach (IModelCreatingDbContextStrategy modelCreatingDbContextStrategy in modelCreatingDbContextStrategies)
                //    modelCreatingDbContextStrategy.Execute(modelBuilder, entityType);

                if (entityType.ClrType.IsAssignableTo(typeof(Entity)))
                {
                    ParameterExpression parameter = Expression.Parameter(entityType.ClrType);

                    Expression<Func<Entity, bool>> filterExpr = entity =>
                        tenant != null &&
                        tenant.TenantId != null &&
                        entity.TenantId == tenant.TenantId;

                    Expression body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
                    LambdaExpression lambdaExpression = Expression.Lambda(body, parameter);

                    entityType.SetQueryFilter(lambdaExpression);
                }
            }

        };
    }
}
