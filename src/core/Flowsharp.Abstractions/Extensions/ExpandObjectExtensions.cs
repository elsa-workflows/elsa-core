using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Flowsharp.Extensions
{
    public static class ExpandObjectExtensions
    {
        public static T Get<T>(this ExpandoObject expandoObject, string key, Func<T> defaultValue = null)
        {
            var dictionary = (IDictionary<string, object>) expandoObject;
            return dictionary.ContainsKey(key) ? (T)dictionary[key] : defaultValue != null ? defaultValue() : default;
        }
    }
}