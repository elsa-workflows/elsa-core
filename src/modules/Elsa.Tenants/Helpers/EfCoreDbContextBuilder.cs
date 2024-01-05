using System.Linq.Expressions;
using Elsa.Common.Contracts;
using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Tenants.Helpers;
public static class EfCoreDbContextBuilder
{
    /// <summary>
    /// Build a <see cref="ElsaDbContextOptions"/> with predefined actions for Tenant management.
    /// </summary>
    public static ElsaDbContextOptions BuildDbContextTenantOption()
    {
        return new ElsaDbContextOptions
        {
            AdditionnalDbContextConstructorAction = BuildDbContextTenantConstructorAction(),
            AdditionnalEntityConfigurations = BuildTenantFilterConfiguration()
        };
    }

    /// <summary>
    /// Build an additionnal constructor action that will fill the TenantId based on the current Tenant in <see cref="ITenantAccessor"/>
    /// </summary>
    public static Action<ElsaDbContextBase, DbContextOptions, IServiceProvider> BuildDbContextTenantConstructorAction()
    {
        return (dbContext, options, serviceProvider) =>
        {
            var tenantAccessor = serviceProvider.GetService<ITenantAccessor>();
            dbContext.TenantId = tenantAccessor?.GetCurrentTenantId();
        };
    }

    /// <summary>
    /// Build a configuration that will be add on EfCore DbContext to filter all the data by the current
    /// </summary>
    /// <returns>Configuration for the DbContext</returns>
    public static Action<ElsaDbContextBase, ModelBuilder, IServiceProvider> BuildTenantFilterConfiguration()
    {
        return (dbContext, modelBuilder, serviceProvider) =>
        {
            //Add global filter on DbContext to split data between tenants
            IEnumerable<IDbContextStrategy>? dbContextStrategies = serviceProvider.GetServices<IDbContextStrategy>();

            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!dbContextStrategies.IsNullOrEmpty())
                {
                    IEnumerable<IModelCreatingDbContextStrategy> modelCreatingDbContextStrategies = dbContextStrategies!
                        .OfType<IModelCreatingDbContextStrategy>()
                        .Where(strategy => strategy.CanExecute(modelBuilder, entityType));

                    foreach (IModelCreatingDbContextStrategy modelCreatingDbContextStrategy in modelCreatingDbContextStrategies)
                        modelCreatingDbContextStrategy.Execute(modelBuilder, entityType);
                }

                if (entityType.ClrType.IsAssignableTo(typeof(Entity)))
                {
                    ParameterExpression parameter = Expression.Parameter(entityType.ClrType);

                    Expression<Func<Entity, bool>> filterExpr = entity => dbContext.TenantId == entity.TenantId;
                    Expression body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
                    LambdaExpression lambdaExpression = Expression.Lambda(body, parameter);

                    entityType.SetQueryFilter(lambdaExpression);
                }
            }

        };
    }
}
