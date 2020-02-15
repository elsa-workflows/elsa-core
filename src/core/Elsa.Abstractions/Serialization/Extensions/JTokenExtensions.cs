using System;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Extensions
{
    public static class JTokenExtensions
    {
        public static T GetValue<T>(this JToken token, string key)
        {
            var value = ((JObject)token).GetValue(key, StringComparison.OrdinalIgnoreCase);

            return value != null ? value.Value<T>() : default;
        }
        
        public static bool HasKey(this JToken token, string key) => ((JObject)token).ContainsKey(key);
    }
}