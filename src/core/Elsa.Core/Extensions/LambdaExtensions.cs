using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Elsa.Extensions
{
    public static class LambdaExtensions
    {
        public static void SetPropertyValue<T, TValue>(this T target, Expression<Func<T, TValue>> expression, TValue value)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                var property = memberExpression.Member as PropertyInfo;
                if (property != null) property.SetValue(target, value, null);
            }
        }
    }
}