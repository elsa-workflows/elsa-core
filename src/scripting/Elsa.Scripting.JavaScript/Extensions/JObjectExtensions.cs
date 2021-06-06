using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting.JavaScript.Extensions
{
    public static class JObjectExtensions
    {
        public static IDictionary<string, object> ToDictionary(this JObject @object)
        {
            var result = @object.ToObject<Dictionary<string, object>>()!;

            var jObjectKeys = (from r in result
                let key = r.Key
                let value = r.Value
                where value != null && value.GetType() == typeof(JObject)
                select key).ToList();

            var jArrayKeys = (from r in result
                let key = r.Key
                let value = r.Value
                where value != null && value.GetType() == typeof(JArray)
                select key).ToList();

            jArrayKeys.ForEach(key => result[key] = ((JArray) result[key]).Values().Select(x =>
            {
                return x is JValue jValue ? jValue.Value : x is JProperty jProperty ? ((JValue) jProperty.Value).Value : x;
            }).ToArray());
            jObjectKeys.ForEach(key => result[key] = ToDictionary((JObject) result[key]));

            return result;
        }
    }
}