using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace Elsa.Scripting.JavaScript.Extensions
{
    public static class ExpandoObjectExtensions
    {
        public static T ToObject<T>(this ExpandoObject expandoObject)
        {
            var dictionary = (IDictionary<string, object>) expandoObject;
            var bindings = new List<MemberBinding>();
            
            foreach (var sourceProperty in typeof(T).GetProperties().Where(x => x.CanWrite))
            {
                var key = dictionary.Keys.SingleOrDefault(x => x.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase));
            
                if (string.IsNullOrEmpty(key)) continue;
                
                var propertyValue = dictionary[key];
                bindings.Add(Expression.Bind(sourceProperty, Expression.Constant(propertyValue)));
            }

            var memberInit = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            return Expression.Lambda<Func<T>>(memberInit).Compile().Invoke();
        }
    }
}