using System.Linq.Expressions;
using Elsa.Extensions;

namespace Elsa.EntityFrameworkCore.Extensions;

public static class ExpressionExtensions
{
    public static Expression<Func<TEntity, bool>> BuildContainsExpression<TEntity>(this Expression<Func<TEntity, string>> keySelector, IEnumerable<TEntity> entities) where TEntity : class
    {
        var compiledKeySelector = keySelector.Compile();
        var list = entities.Select(compiledKeySelector);
        var property = keySelector.GetProperty()!;
        var param = Expression.Parameter(typeof(TEntity));
        
        var body = Expression.Call(
            typeof(Enumerable), 
            "Contains", 
            new[] {compiledKeySelector.Method.ReturnType},
            Expression.Constant(list), Expression.Property(param, property));
        
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    public static Expression<Func<TEntity, bool>> BuildEqualsExpresion<TEntity>(this Expression<Func<TEntity, string>> keySelector, TEntity entity)
    {
        var keyName = keySelector.GetProperty()!.Name;

        // Define parameters for the lambda expression
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var keySelectorLambda = Expression.Lambda<Func<TEntity, string>>(Expression.Property(parameter, keyName), parameter);

        // Build the expression that compares the keys
        var entityKey = keySelectorLambda.Compile()(entity);
        var comparison = Expression.Equal(keySelectorLambda.Body, Expression.Constant(entityKey));

        // Create the final lambda expression that can be used in AnyAsync
        var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);

        return lambda;
    }
}