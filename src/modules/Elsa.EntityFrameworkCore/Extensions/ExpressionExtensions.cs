using System.Linq.Expressions;
using System.Reflection;

namespace Elsa.EntityFrameworkCore.Extensions;

public static class ExpressionExtensions
{
    public static PropertyInfo GetPropertyInfo<TType, TReturn>(this Expression<Func<TType, TReturn>> propertyExpression)
    {
        LambdaExpression lambda = propertyExpression;
        var memberExpression = lambda.Body is UnaryExpression expression
            ? (MemberExpression) expression.Operand
            : (MemberExpression) lambda.Body;

        return (PropertyInfo) memberExpression.Member;
    }
    
    public static Expression<Func<TEntity, bool>> BuildContainsExpression<TEntity>(this Func<TEntity, object> uniqueFieldDelegate, IEnumerable<TEntity> entities, PropertyInfo property) where TEntity : class
    {
        var list = entities.Select(uniqueFieldDelegate.Invoke);
        var param = Expression.Parameter(typeof(TEntity));
        var body = Expression.Call(
            typeof(Enumerable), 
            "Contains", 
            new[] {uniqueFieldDelegate.Method.ReturnType},
            Expression.Constant(list), Expression.Property(param, property));
        
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }
}