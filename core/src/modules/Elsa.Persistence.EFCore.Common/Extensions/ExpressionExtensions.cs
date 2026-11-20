using System.Linq.Expressions;
using System.Reflection;
using Elsa.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Expression"/> objects.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Builds an expression that checks if the specified property is contained in the specified list of entities.
    /// </summary>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="entities">The entities.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The expression.</returns>
    public static Expression<Func<TEntity, bool>> BuildContainsExpression<TEntity>(this Expression<Func<TEntity, string>> keySelector, IEnumerable<TEntity> entities) where TEntity : class
    {
        var compiledKeySelector = keySelector.Compile();
        var list = entities.Select(compiledKeySelector).ToList();
        var property = keySelector.GetProperty()!;
        var param = Expression.Parameter(typeof(TEntity));
        
        var body = Expression.Call(
            typeof(Enumerable), 
            "Contains", 
            new[] {compiledKeySelector.Method.ReturnType},
            Expression.Constant(list), Expression.Property(param, property));
        
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    /// <summary>
    /// Builds an expression that checks if the specified property is equal to the specified entity's property.
    /// </summary>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="entity">The entity.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The expression.</returns>
    public static Expression<Func<TEntity, bool>> BuildEqualsExpression<TEntity>(this Expression<Func<TEntity, string>> keySelector, TEntity entity)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, keySelector.GetPropertyName());
        var key = (string?)((PropertyInfo)property.Member).GetValue(entity);

        // A captured key becomes a query parameter, so EF reuses the lookup for different IDs.
        Expression<Func<string?>> keyValue = () => key;
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(property, keyValue.Body), parameter);
    }
}