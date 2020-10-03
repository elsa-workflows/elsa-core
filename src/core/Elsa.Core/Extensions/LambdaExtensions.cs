using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Elsa.Extensions
{
    public static class LambdaExtensions
    {
        public static void SetPropertyValue<T, TProperty>(this T target, Expression<Func<T, TProperty>> expression, TProperty value)
        {
            var property = expression.GetProperty();
            
            if (property != null) 
                property.SetValue(target, value, null);
        }
        
        public static PropertyInfo? GetProperty<T, TProperty>(this Expression<Func<T, TProperty>> expression) =>
            expression.Body is MemberExpression memberExpression
                ? memberExpression.Member as PropertyInfo
                : default;
    }
}